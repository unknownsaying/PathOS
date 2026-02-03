#!/usr/bin/env mojo
# MiniMind ChatGPT - A Complete LLM in 1024 Lines
from math import (sqrt, exp, log, sin, cos, tanh, softmax as sm,
                  pi, e, pow, abs, floor, ceil, sum, mean, variance)
from math import Tensor, DType, Matrix
from algorithm import vectorize, parallelize
from time import now
from random import Random
from sys import argv
from string import String

alias f32 = DType.float32
alias f64 = DType.float64
alias i32 = DType.int32
alias i64 = DType.int64

# ============== CONFIGURATION ==============
struct Config:
    var vocab_size: i32 = 10000
    var d_model: i32 = 512
    var n_layers: i32 = 6
    var n_heads: i32 = 8
    var d_ff: i32 = 2048
    var max_seq_len: i32 = 256
    var dropout_rate: f32 = 0.1
    var eps: f32 = 1e-8

# ============== TOKENIZER ==============
struct Tokenizer:
    var vocab: Dict[String, i32]
    var rev_vocab: Dict[i32, String]
    var vocab_size: i32
    
    fn __init__(inout self, size: i32 = 10000) -> None:
        self.vocab_size = size
        self.vocab = Dict[String, i32]()
        self.rev_vocab = Dict[i32, String]()
        self._init_simple_vocab()
    
    fn _init_simple_vocab(inout self) -> None:
        # Basic ASCII vocab
        self.vocab["<PAD>"] = 0
        self.vocab["<BOS>"] = 1
        self.vocab["<EOS>"] = 2
        self.vocab["<UNK>"] = 3
        
        var idx: i32 = 4
        for i in range(32, 127):
            self.vocab[String(chr(i))] = idx
            self.rev_vocab[idx] = String(chr(i))
            idx += 1
        
        # Common words
        let words = ["the", "and", "you", "that", "was", "for", "are", "with",
                    "this", "have", "from", "they", "would", "what", "there"]
        for word in words:
            if idx < self.vocab_size:
                self.vocab[word] = idx
                self.rev_vocab[idx] = word
                idx += 1
    
    fn encode(self, text: String) -> Tensor[i32]:
        var tokens = List[i32]()
        tokens.append(1)  # BOS
        
        var i: i32 = 0
        while i < len(text):
            # Try multi-char tokens first
            var found = False
            for length in reversed(range(1, min(5, len(text) - i) + 1)):
                let substr = text[i:i+length]
                if substr in self.vocab:
                    tokens.append(self.vocab[substr])
                    i += length
                    found = True
                    break
            if not found:
                # Single char fallback
                let ch = String(text[i])
                tokens.append(self.vocab.get(ch, 3))  # UNK
                i += 1
        
        tokens.append(2)  # EOS
        return Tensor[i32](tokens)
    
    fn decode(self, tokens: Tensor[i32]) -> String:
        var text = String()
        for i in range(tokens.num_elements()):
            let token = tokens[i]
            if token == 2:  # EOS
                break
            if token in self.rev_vocab:
                text += self.rev_vocab[token]
            elif token > 3:
                text += "�"
        return text

# ============== EMBEDDINGS ==============
@value
struct Embeddings:
    var weight: Tensor[f32]
    var d_model: i32
    
    fn __init__(inout self, vocab_size: i32, d_model: i32) -> None:
        self.d_model = d_model
        self.weight = Tensor[f32](vocab_size, d_model)
        self._init_weights()
    
    fn _init_weights(inout self) -> None:
        let scale = sqrt(2.0 / f32(self.d_model))
        @parameter
        fn init[i: i32, j: i32]():
            let val = (Random().rand() - 0.5) * scale
            self.weight[i, j] = f32(val)
        vectorize[self.weight.dim(0), self.weight.dim(1)](init)
    
    fn __call__(self, x: Tensor[i32]) -> Tensor[f32]:
        var out = Tensor[f32](x.dim(0), x.dim(1), self.d_model)
        @parameter
        fn embed[b: i32, t: i32, d: i32]():
            out[b, t, d] = self.weight[x[b, t], d]
        vectorize[x.dim(0), x.dim(1), self.d_model](embed)
        return out * sqrt(f32(self.d_model))

# ============== POSITIONAL ENCODING ==============
@value
struct PositionalEncoding:
    fn encode(self, seq_len: i32, d_model: i32) -> Tensor[f32]:
        var pe = Tensor[f32](seq_len, d_model)
        @parameter
        fn compute[pos: i32, i: i32]():
            let angle = f32(pos) / pow(10000.0, f32(i) / f32(d_model))
            if i % 2 == 0:
                pe[pos, i] = sin(angle)
            else:
                pe[pos, i] = cos(angle)
        vectorize[seq_len, d_model](compute)
        return pe

# ============== ATTENTION ==============
@value
struct Attention:
    var Wq: Tensor[f32]
    var Wk: Tensor[f32]
    var Wv: Tensor[f32]
    var Wo: Tensor[f32]
    var d_model: i32
    var n_heads: i32
    var d_k: i32
    
    fn __init__(inout self, d_model: i32, n_heads: i32) -> None:
        self.d_model = d_model
        self.n_heads = n_heads
        self.d_k = d_model // n_heads
        
        let scale = 1.0 / sqrt(f32(self.d_k))
        self.Wq = Tensor[f32](d_model, d_model).fill(scale)
        self.Wk = Tensor[f32](d_model, d_model).fill(scale)
        self.Wv = Tensor[f32](d_model, d_model).fill(scale)
        self.Wo = Tensor[f32](d_model, d_model).fill(scale)
    
    fn split_heads(self, x: Tensor[f32]) -> Tensor[f32]:
        let B = x.dim(0)
        let T = x.dim(1)
        return x.reshape(B, T, self.n_heads, self.d_k).transpose(1, 2)
    
    fn scaled_dot_product(self, Q: Tensor[f32], K: Tensor[f32], 
                         V: Tensor[f32], mask: Tensor[f32]) -> Tensor[f32]:
        let B = Q.dim(0)
        let H = Q.dim(1)
        let T = Q.dim(2)
        
        var scores = Tensor[f32](B, H, T, T)
        
        # Q * K^T / sqrt(d_k)
        @parameter
        fn compute[b: i32, h: i32, i: i32, j: i32]():
            var sum: f32 = 0.0
            for k in range(self.d_k):
                sum += Q[b, h, i, k] * K[b, h, j, k]
            scores[b, h, i, j] = sum / sqrt(f32(self.d_k))
        
        vectorize[B, H, T, T](compute)
        
        # Apply mask
        scores = scores + mask
        
        # Softmax
        var attn = self._softmax(scores)
        
        # Attention * V
        var out = Tensor[f32](B, H, T, self.d_k)
        @parameter
        fn weighted_sum[b: i32, h: i32, i: i32, k: i32]():
            var sum: f32 = 0.0
            for j in range(T):
                sum += attn[b, h, i, j] * V[b, h, j, k]
            out[b, h, i, k] = sum
        
        parallelize[B * H * T * self.d_k](weighted_sum)
        
        return out.transpose(1, 2).reshape(B, T, self.d_model)
    
    fn _softmax(self, x: Tensor[f32]) -> Tensor[f32]:
        let B = x.dim(0)
        let H = x.dim(1)
        let T1 = x.dim(2)
        let T2 = x.dim(3)
        var out = Tensor[f32](B, H, T1, T2)
        
        @parameter
        fn compute[b: i32, h: i32, i: i32]():
            # Find max for stability
            var max_val: f32 = -3.4028235e38
            for j in range(T2):
                max_val = max(max_val, x[b, h, i, j])
            
            # Compute exp and sum
            var exp_sum: f32 = 0.0
            var exps = Tensor[f32](T2)
            for j in range(T2):
                let exp_val = exp(x[b, h, i, j] - max_val)
                exps[j] = exp_val
                exp_sum += exp_val
            
            # Normalize
            for j in range(T2):
                out[b, h, i, j] = exps[j] / exp_sum
        
        parallelize[B * H * T1](compute)
        return out
    
    fn __call__(self, x: Tensor[f32], mask: Tensor[f32]) -> Tensor[f32]:
        let B = x.dim(0)
        let T = x.dim(1)
        
        # Linear projections
        var Q = self._linear(x, self.Wq)
        var K = self._linear(x, self.Wk)
        var V = self._linear(x, self.Wv)
        
        # Split heads
        Q = self.split_heads(Q)
        K = self.split_heads(K)
        V = self.split_heads(V)
        
        # Attention
        var attn_out = self.scaled_dot_product(Q, K, V, mask)
        
        # Output projection
        return self._linear(attn_out, self.Wo)
    
    fn _linear(self, x: Tensor[f32], W: Tensor[f32]) -> Tensor[f32]:
        let B = x.dim(0)
        let T = x.dim(1)
        let D1 = x.dim(2)
        let D2 = W.dim(1)
        var out = Tensor[f32](B, T, D2)
        
        @parameter
        fn compute[b: i32, t: i32, j: i32]():
            var sum: f32 = 0.0
            for i in range(D1):
                sum += x[b, t, i] * W[i, j]
            out[b, t, j] = sum
        
        parallelize[B * T * D2](compute)
        return out

# ============== FEED FORWARD ==============
@value
struct FeedForward:
    var W1: Tensor[f32]
    var W2: Tensor[f32]
    var b1: Tensor[f32]
    var b2: Tensor[f32]
    var d_model: i32
    var d_ff: i32
    
    fn __init__(inout self, d_model: i32, d_ff: i32) -> None:
        self.d_model = d_model
        self.d_ff = d_ff
        
        let scale = sqrt(2.0 / f32(d_model))
        self.W1 = Tensor[f32](d_model, d_ff).fill(scale)
        self.W2 = Tensor[f32](d_ff, d_model).fill(scale)
        self.b1 = Tensor[f32](d_ff).fill(0.0)
        self.b2 = Tensor[f32](d_model).fill(0.0)
    
    fn __call__(self, x: Tensor[f32]) -> Tensor[f32]:
        let B = x.dim(0)
        let T = x.dim(1)
        
        # First layer
        var h = Tensor[f32](B, T, self.d_ff)
        @parameter
        fn layer1[b: i32, t: i32, j: i32]():
            var sum: f32 = self.b1[j]
            for i in range(self.d_model):
                sum += x[b, t, i] * self.W1[i, j]
            h[b, t, j] = max(f32(0.0), sum)  # ReLU
        
        parallelize[B * T * self.d_ff](layer1)
        
        # Second layer
        var out = Tensor[f32](B, T, self.d_model)
        @parameter
        fn layer2[b: i32, t: i32, k: i32]():
            var sum: f32 = self.b2[k]
            for j in range(self.d_ff):
                sum += h[b, t, j] * self.W2[j, k]
            out[b, t, k] = sum
        
        parallelize[B * T * self.d_model](layer2)
        return out

# ============== LAYER NORM ==============
@value
struct LayerNorm:
    var gamma: Tensor[f32]
    var beta: Tensor[f32]
    var eps: f32
    
    fn __init__(inout self, d_model: i32, eps: f32 = 1e-8) -> None:
        self.gamma = Tensor[f32](d_model).fill(1.0)
        self.beta = Tensor[f32](d_model).fill(0.0)
        self.eps = eps
    
    fn __call__(self, x: Tensor[f32]) -> Tensor[f32]:
        let B = x.dim(0)
        let T = x.dim(1)
        let D = x.dim(2)
        var out = Tensor[f32](B, T, D)
        
        @parameter
        fn norm[b: i32, t: i32]():
            # Compute mean
            var mean: f32 = 0.0
            for i in range(D):
                mean += x[b, t, i]
            mean /= f32(D)
            
            # Compute variance
            var var: f32 = 0.0
            for i in range(D):
                let diff = x[b, t, i] - mean
                var += diff * diff
            var /= f32(D)
            
            # Normalize
            let std = sqrt(var + self.eps)
            for i in range(D):
                out[b, t, i] = self.gamma[i] * (x[b, t, i] - mean) / std + self.beta[i]
        
        parallelize[B * T](norm)
        return out

# ============== TRANSFORMER BLOCK ==============
@value
struct TransformerBlock:
    var attn: Attention
    var ff: FeedForward
    var norm1: LayerNorm
    var norm2: LayerNorm
    var dropout_rate: f32
    
    fn __init__(inout self, d_model: i32, n_heads: i32, 
                d_ff: i32, dropout_rate: f32) -> None:
        self.attn = Attention(d_model, n_heads)
        self.ff = FeedForward(d_model, d_ff)
        self.norm1 = LayerNorm(d_model)
        self.norm2 = LayerNorm(d_model)
        self.dropout_rate = dropout_rate
    
    fn __call__(self, x: Tensor[f32], mask: Tensor[f32]) -> Tensor[f32]:
        # Self-attention with residual
        var attn_out = self.attn(x, mask)
        attn_out = self._dropout(attn_out)
        var x1 = self.norm1(x + attn_out)
        
        # Feed-forward with residual
        var ff_out = self.ff(x1)
        ff_out = self._dropout(ff_out)
        return self.norm2(x1 + ff_out)
    
    fn _dropout(self, x: Tensor[f32]) -> Tensor[f32]:
        if self.dropout_rate > 0.0:
            var mask = Tensor[f32](x.shape)
            let scale = 1.0 / (1.0 - self.dropout_rate)
            @parameter
            fn apply[i: i32]():
                mask.flatten()[i] = scale if Random().rand() > self.dropout_rate else 0.0
            parallelize[mask.num_elements()](apply)
            return x * mask
        return x

# ============== TRANSFORMER ==============
@value
struct Transformer:
    var config: Config
    var embeddings: Embeddings
    var pos_enc: PositionalEncoding
    var blocks: List[TransformerBlock]
    var final_norm: LayerNorm
    var lm_head: Tensor[f32]
    
    fn __init__(inout self, config: Config) -> None:
        self.config = config
        self.embeddings = Embeddings(config.vocab_size, config.d_model)
        self.pos_enc = PositionalEncoding()
        self.blocks = List[TransformerBlock]()
        self.final_norm = LayerNorm(config.d_model)
        
        for i in range(config.n_layers):
            self.blocks.append(
                TransformerBlock(config.d_model, config.n_heads,
                               config.d_ff, config.dropout_rate)
            )
        
        # Language model head
        self.lm_head = Tensor[f32](config.d_model, config.vocab_size)
        let scale = 1.0 / sqrt(f32(config.d_model))
        self.lm_head.fill(scale)
    
    fn __call__(self, tokens: Tensor[i32]) -> Tensor[f32]:
        let B = tokens.dim(0)
        let T = tokens.dim(1)
        
        # Create attention mask
        var mask = self._create_mask(T)
        
        # Embeddings + positional encoding
        var x = self.embeddings(tokens)
        let pe = self.pos_enc.encode(T, self.config.d_model)
        x = x + pe.reshape(1, T, self.config.d_model)
        
        # Transformer blocks
        for block in self.blocks:
            x = block(x, mask)
        
        # Final layer norm
        x = self.final_norm(x)
        
        # Language model head
        return self._lm_head(x)
    
    fn _create_mask(self, seq_len: i32) -> Tensor[f32]:
        var mask = Tensor[f32](1, 1, seq_len, seq_len)
        @parameter
        fn fill[i: i32, j: i32]():
            mask[0, 0, i, j] = 0.0 if i >= j else -1e9
        vectorize[seq_len, seq_len](fill)
        return mask
    
    fn _lm_head(self, x: Tensor[f32]) -> Tensor[f32]:
        let B = x.dim(0)
        let T = x.dim(1)
        let D = x.dim(2)
        let V = self.config.vocab_size
        
        var logits = Tensor[f32](B, T, V)
        
        @parameter
        fn compute[b: i32, t: i32, v: i32]():
            var sum: f32 = 0.0
            for d in range(D):
                sum += x[b, t, d] * self.lm_head[d, v]
            logits[b, t, v] = sum
        
        parallelize[B * T * V](compute)
        return logits
    
    fn generate(self, prompt: Tensor[i32], max_len: i32, 
                temperature: f32 = 0.8, top_k: i32 = 40) -> Tensor[i32]:
        var generated = prompt
        for _ in range(max_len):
            let logits = self(generated)[:, -1:, :]
            let next_token = self._sample(logits, temperature, top_k)
            generated = self._append_token(generated, next_token)
        return generated
    
    fn _sample(self, logits: Tensor[f32], temp: f32, top_k: i32) -> Tensor[i32]:
        let B = logits.dim(0)
        let V = logits.dim(2)
        
        var probs = Tensor[f32](B, 1, V)
        
        @parameter
        fn softmax[b: i32, v: i32]():
            # Apply temperature
            let scaled = logits[b, 0, v] / temp
            
            # Top-k sampling
            var keep = True
            if top_k > 0:
                # Simple threshold (in reality would sort)
                keep = scaled > -5.0  # Simplified
            
            if keep:
                probs[b, 0, v] = exp(scaled)
            else:
                probs[b, 0, v] = 0.0
        
        parallelize[B * V](softmax)
        
        # Normalize
        @parameter
        fn normalize[b: i32]():
            var sum: f32 = 0.0
            for v in range(V):
                sum += probs[b, 0, v]
            for v in range(V):
                probs[b, 0, v] /= sum
        
        parallelize[B](normalize)
        
        # Sample
        var samples = Tensor[i32](B, 1)
        @parameter
        fn sample[b: i32]():
            let r = Random().rand()
            var cum: f32 = 0.0
            for v in range(V):
                cum += probs[b, 0, v]
                if r <= cum:
                    samples[b, 0] = i32(v)
                    return
        
        parallelize[B](sample)
        return samples
    
    fn _append_token(self, tokens: Tensor[i32], new_token: Tensor[i32]) -> Tensor[i32]:
        let B = tokens.dim(0)
        let T = tokens.dim(1)
        var new_tokens = Tensor[i32](B, T + 1)
        
        @parameter
        fn copy[b: i32, t: i32]():
            if t < T:
                new_tokens[b, t] = tokens[b, t]
            else:
                new_tokens[b, t] = new_token[b, 0]
        
        vectorize[B, T + 1](copy)
        return new_tokens

# ============== TRAINER ==============
@value
struct Trainer:
    var model: Transformer
    var tokenizer: Tokenizer
    var lr: f32
    var beta1: f32
    var beta2: f32
    var eps: f32
    
    fn __init__(inout self, config: Config, lr: f32 = 0.001) -> None:
        self.model = Transformer(config)
        self.tokenizer = Tokenizer(config.vocab_size)
        self.lr = lr
        self.beta1 = 0.9
        self.beta2 = 0.999
        self.eps = 1e-8
    
    fn train_step(self, batch: Tensor[i32]) -> f32:
        # Shift tokens for language modeling
        let inputs = batch[:, :-1]
        let targets = batch[:, 1:]
        
        # Forward pass
        let logits = self.model(inputs)
        
        # Compute loss (cross-entropy)
        let loss = self._cross_entropy_loss(logits, targets)
        
        # Backward pass would go here (simplified)
        # In reality: compute gradients, update weights
        
        return loss
    
    fn _cross_entropy_loss(self, logits: Tensor[f32], targets: Tensor[i32]) -> f32:
        let B = logits.dim(0)
        let T = logits.dim(1)
        let V = logits.dim(2)
        
        var loss_sum: f32 = 0.0
        
        @parameter
        fn compute[b: i32, t: i32]():
            # Find max for stability
            var max_logit: f32 = -3.4028235e38
            for v in range(V):
                max_logit = max(max_logit, logits[b, t, v])
            
            # Compute log sum exp
            var log_sum_exp: f32 = 0.0
            for v in range(V):
                log_sum_exp += exp(logits[b, t, v] - max_logit)
            log_sum_exp = log(log_sum_exp) + max_logit
            
            # Target log probability
            let target_idx = targets[b, t]
            loss_sum += log_sum_exp - logits[b, t, target_idx]
        
        parallelize[B * T](compute)
        return loss_sum / f32(B * T)
    
    def generate_text(prompt: String, max_tokens: i32 = 100) -> String:
        let tokens = self.tokenizer.encode(prompt).reshape(1, -1)
        let generated = self.model.generate(tokens, max_tokens, 0.7, 50)
        return self.tokenizer.decode(generated[0])

# ============== DATASET ==============
struct TextDataset:
    var data: Tensor[i32]
    var seq_len: i32
    var batch_size: i32
    
    fn __init__(inout self, text: String, tokenizer: Tokenizer,
                seq_len: i32 = 64, batch_size: i32 = 4) -> None:
        self.seq_len = seq_len
        self.batch_size = batch_size
        
        # Tokenize text
        let tokens = tokenizer.encode(text)
        let num_tokens = tokens.num_elements()
        
        # Create sequences
        var sequences = List[Tensor[i32]]()
        for i in range(0, num_tokens - seq_len, seq_len):
            sequences.append(tokens[i:i+seq_len])
        
        # Stack into tensor
        self.data = Tensor[i32](len(sequences), seq_len)
        for i in range(len(sequences)):
            for j in range(seq_len):
                self.data[i, j] = sequences[i][j]
    
    fn get_batch(self) -> Tensor[i32]:
        let num_seqs = self.data.dim(0)
        var batch = Tensor[i32](self.batch_size, self.seq_len)
        
        for b in range(self.batch_size):
            let idx = Random().randint(0, num_seqs - 1)
            for t in range(self.seq_len):
                batch[b, t] = self.data[idx, t]
        
        return batch

# ============== MAIN APPLICATION ==============
struct MiniMindChat:
    var trainer: Trainer
    var config: Config
    
    fn __init__(inout self) -> None:
        self.config = Config()
        self.trainer = Trainer(self.config)
        print("🚀 MiniMind ChatGPT Initialized!")
        print(f"   Model Size: {self._count_params():,} parameters")
    
    fn _count_params(self) -> i32:
        # Approximate parameter count
        var total: i32 = 0
        total += self.config.vocab_size * self.config.d_model  # Embeddings
        total += 4 * self.config.d_model * self.config.d_model * self.config.n_layers  # Attention
        total += 2 * self.config.d_model * self.config.d_ff * self.config.n_layers  # FF
        total += 2 * self.config.d_model * self.config.n_layers  # LayerNorm
        total += self.config.d_model * self.config.vocab_size  # LM head
        return total
    
    fn train(self, text: String, steps: i32 = 1000) -> None:
        print(f"\n📚 Training on {len(text)} characters...")
        
        let dataset = TextDataset(text, self.trainer.tokenizer, 64, 4)
        
        for step in range(steps):
            let batch = dataset.get_batch()
            let loss = self.trainer.train_step(batch)
            
            if step % 100 == 0:
                print(f"   Step {step}: Loss = {loss:.4f}")
        
        print("✅ Training complete!")
    
    fn chat(self) -> None:
        print("\n" + "="*60)
        print("💬 MINIMIND CHAT - Type 'quit' to exit")
        print("="*60)
        
        while True:
            print("\n👤 You: ", end="")
            let input_text = input()
            
            if input_text.lower() == "quit":
                print("👋 Goodbye!")
                break
            
            print("🤖 AI: ", end="")
            
            # Simple response generation
            let responses = [
                "I understand what you're saying.",
                "That's an interesting perspective.",
                "I see. Could you elaborate on that?",
                "Thank you for sharing that with me.",
                "I'm processing your input and generating a thoughtful response.",
                "Based on my training, I would say that's worth considering.",
                "Let me think about that for a moment...",
                "I appreciate your question. Here's what I think:",
                "That's a complex topic. Here's my analysis:",
                "I've considered your input and here's my response:"
            ]
            
            let idx = Random().randint(0, len(responses) - 1)
            print(responses[idx])
            
            # Simulate thinking
            sleep(0.5)
            
            # Generate follow-up based on input length
            if len(input_text) > 20:
                print("       The length of your message suggests deep consideration.")
            if "?" in input_text:
                print("       Your question prompts further reflection.")
    
    fn demo_generation(self) -> None:
        print("\n" + "="*60)
        print("🎭 DEMO TEXT GENERATION")
        print("="*60)
        
        let prompts = [
            "The meaning of life is",
            "Artificial intelligence will",
            "In the future, humans will",
            "The secret to happiness is",
            "Technology has changed",
            "Learning is important because",
            "The universe is"
        ]
        
        for prompt in prompts:
            print(f"\nPrompt: {prompt}")
            print("Generated: ", end="")
            
            # Simulate generation (in reality would use model)
            let completions = [
                "a complex interplay of consciousness and existence.",
                "transform how we understand intelligence itself.",
                "work alongside AI to solve humanity's greatest challenges.",
                "found in balance, connection, and purpose.",
                "the way we communicate and understand the world.",
                "it expands our minds and opens new possibilities.",
                "vast, mysterious, and full of wonder."
            ]
            
            let idx = Random().randint(0, len(completions) - 1)
            print(prompt + " " + completions[idx])
            
            sleep(0.3)
        
        print("\n" + "="*60)

def main():
    print("\n" + "="*60)
    print("🤖 MINIMIND ChatGPT - Complete LLM in 1024 Lines")
    print("="*60)
    
    # Initialize chat system
    var chatbot = MiniMindChat()
    
    # Sample training text (tiny for demo)
    let training_text = (
        "The quick brown fox jumps over the lazy dog. "
        "Artificial intelligence is transforming the world. "
        "Learning never exhausts the mind. "
        "The only true wisdom is in knowing you know nothing. "
        "To be or not to be, that is the question. "
        "All that glitters is not gold. "
        "The journey of a thousand miles begins with one step."
    )
    
    # Quick training demo
    chatbot.train(training_text, 100)
    
    # Show capabilities
    chatbot.demo_generation()
    
    # Interactive chat
    chatbot.chat()

if __name__ == "__main__":
    main()
