; ============================================================================
; LINUX KERNEL CORE (x86-64 Assembly)
; ============================================================================
; This represents a simplified Linux kernel core with:
; - Task scheduling
; - Memory management
; - System call handling
; - Interrupt handling
; ============================================================================

[BITS 64]
[ORG 0xFFFFFF8000000000]  ; Linux kernel virtual address space

section .text
global _start
global kernel_main
global schedule
global syscall_handler
global page_fault_handler

; ============================================================================
; LINUX KERNEL CONSTANTS AND STRUCTURES
; ============================================================================

; Task states
TASK_RUNNING     equ 0
TASK_INTERRUPTIBLE equ 1
TASK_UNINTERRUPTIBLE equ 2
TASK_ZOMBIE      equ 4
TASK_STOPPED     equ 8

; Process Control Block (simplified)
struc task_struct
    .state:         resq 1    ; Process state
    .counter:       resq 1    ; Time slice counter
    .priority:      resq 1    ; Static priority
    .mm:            resq 1    ; Memory management struct
    .thread:        resq 1    ; CPU-specific state
    .stack:         resq 1    ; Kernel stack pointer
    .cpu_context:   resq 18   ; CPU registers (for context switch)
    .pid:           resd 1    ; Process ID
    .tgid:          resd 1    ; Thread group ID
    .flags:         resq 1    ; Process flags
    .ptrace:        resq 1    ; For ptrace
    .real_parent:   resq 1    ; Real parent process
    .parent:        resq 1    ; Parent process
    .children:      resq 1    ; List of children
    .sibling:       resq 1    ; Link in parent's children list
    .group_leader:  resq 1    ; Thread group leader
    .tasks:         resq 1    ; List of all tasks
    .size:
endstruc

; Memory page flags
PG_PRESENT       equ 1 << 0
PG_WRITE         equ 1 << 1
PG_USER          equ 1 << 2
PG_PWT           equ 1 << 3
PG_PCD           equ 1 << 4
PG_ACCESSED      equ 1 << 5
PG_DIRTY         equ 1 << 6
PG_PSE           equ 1 << 7
PG_GLOBAL        equ 1 << 8
PG_NX            equ 1 << 63  ; No-execute bit

; ============================================================================
; KERNEL ENTRY POINT
; ============================================================================

_start:
    ; Clear direction flag
    cld
    
    ; Set up stack
    mov rsp, kernel_stack_top
    
    ; Set up segment registers
    mov ax, 0x10      ; Kernel data segment
    mov ds, ax
    mov es, ax
    mov fs, ax
    mov gs, ax
    mov ss, ax
    
    ; Initialize memory management
    call init_memory
    
    ; Initialize interrupt descriptor table
    call init_idt
    
    ; Initialize task scheduler
    call init_scheduler
    
    ; Call main kernel function
    call kernel_main
    
    ; Should never return, but if it does, halt
    cli
    hlt

; ============================================================================
; KERNEL MAIN FUNCTION
; ============================================================================

kernel_main:
    push rbp
    mov rbp, rsp
    
    ; Print kernel boot message
    mov rdi, kernel_boot_msg
    call kernel_print
    
    ; Initialize system calls
    call init_syscalls
    
    ; Initialize devices
    call init_devices
    
    ; Create init process
    call create_init_process
    
    ; Enable interrupts
    sti
    
    ; Start scheduler
    call scheduler_loop
    
    pop rbp
    ret

; ============================================================================
; SCHEDULER IMPLEMENTATION
; ============================================================================

; Simple round-robin scheduler
schedule:
    push rbp
    mov rbp, rsp
    push rbx
    push r12
    push r13
    push r14
    push r15
    
    ; Disable interrupts
    cli
    
    ; Get current task
    call get_current_task
    mov rbx, rax        ; rbx = current task
    
    ; If task is running, update its counter
    cmp qword [rbx + task_struct.state], TASK_RUNNING
    jne .skip_counter_update
    
    ; Update counter and check if it's zero
    dec qword [rbx + task_struct.counter]
    jnz .skip_reschedule
    
.skip_counter_update:
    ; Find next task to run
    mov rax, task_list
    mov rcx, [rax]      ; First task in list
    
.find_next_task:
    test rcx, rcx
    jz .idle_task       ; No tasks, run idle
    
    ; Check if task is runnable
    cmp qword [rcx + task_struct.state], TASK_RUNNING
    je .switch_task
    
    ; Move to next task
    mov rcx, [rcx + task_struct.tasks]
    jmp .find_next_task

.switch_task:
    ; Switch to new task
    mov rdi, rbx        ; Old task
    mov rsi, rcx        ; New task
    call context_switch
    jmp .schedule_done

.idle_task:
    ; Run idle task (simplified - just halt)
    sti
    hlt
    cli
    jmp .schedule_done

.skip_reschedule:
    ; Keep running current task
    sti
    jmp .schedule_done

.schedule_done:
    ; Restore registers and return
    pop r15
    pop r14
    pop r13
    pop r12
    pop rbx
    pop rbp
    ret

; Context switch between tasks
context_switch:
    push rbp
    mov rbp, rsp
    
    ; Save old task context
    ; Save registers to old task's cpu_context
    mov [rdi + task_struct.cpu_context + 0], rbx
    mov [rdi + task_struct.cpu_context + 8], rsp
    mov [rdi + task_struct.cpu_context + 16], rbp
    mov [rdi + task_struct.cpu_context + 24], r12
    mov [rdi + task_struct.cpu_context + 32], r13
    mov [rdi + task_struct.cpu_context + 40], r14
    mov [rdi + task_struct.cpu_context + 48], r15
    
    ; Save instruction pointer
    mov rax, [rbp + 8]   ; Return address
    mov [rdi + task_struct.cpu_context + 56], rax
    
    ; Load new task context
    mov rbx, [rsi + task_struct.cpu_context + 0]
    mov rsp, [rsi + task_struct.cpu_context + 8]
    mov rbp, [rsi + task_struct.cpu_context + 16]
    mov r12, [rsi + task_struct.cpu_context + 24]
    mov r13, [rsi + task_struct.cpu_context + 32]
    mov r14, [rsi + task_struct.cpu_context + 40]
    mov r15, [rsi + task_struct.cpu_context + 48]
    
    ; Set current task
    mov [current_task], rsi
    
    ; Jump to new task's instruction pointer
    mov rax, [rsi + task_struct.cpu_context + 56]
    push rax
    
    pop rbp
    ret

; Scheduler main loop
scheduler_loop:
.loop:
    call schedule
    
    ; Check for pending signals
    call check_signals
    
    ; Handle work queues
    call process_workqueues
    
    jmp .loop

; ============================================================================
; SYSTEM CALL HANDLER
; ============================================================================

syscall_handler:
    ; Save registers
    push r15
    push r14
    push r13
    push r12
    push r11
    push r10
    push r9
    push r8
    push rbp
    push rdi
    push rsi
    push rdx
    push rcx
    push rbx
    push rax
    
    ; Get system call number from rax
    mov rbx, rax
    
    ; Validate system call number
    cmp rbx, MAX_SYSCALLS
    jae .invalid_syscall
    
    ; Get current task
    call get_current_task
    mov rbp, rax
    
    ; Check if user has permission
    ; (Simplified - always allow for now)
    
    ; Call system call handler
    shl rbx, 3           ; Multiply by 8 (size of function pointer)
    mov rax, [syscall_table + rbx]
    call rax
    
    ; Return value is in rax
    mov [rsp + 0], rax   ; Store in saved rax
    
    ; Restore registers
    pop rax
    pop rbx
    pop rcx
    pop rdx
    pop rsi
    pop rdi
    pop rbp
    pop r8
    pop r9
    pop r10
    pop r11
    pop r12
    pop r13
    pop r14
    pop r15
    
    ; Return to user mode
    o64 sysret

.invalid_syscall:
    mov rax, -1          ; Return -ENOSYS
    jmp syscall_handler  ; Jump to restore registers

; ============================================================================
; MEMORY MANAGEMENT
; ============================================================================

; Page fault handler
page_fault_handler:
    push rax
    push rbx
    push rcx
    push rdx
    
    ; Get faulting address from CR2
    mov rax, cr2
    
    ; Get error code from stack
    mov rbx, [rsp + 32]  ; Error code pushed by CPU
    
    ; Check if it's a user-mode access
    test rbx, 4          ; User mode bit
    jz .kernel_fault
    
    ; User-mode page fault
    call handle_user_page_fault
    jmp .page_fault_done

.kernel_fault:
    ; Kernel-mode page fault
    call handle_kernel_page_fault

.page_fault_done:
    pop rdx
    pop rcx
    pop rbx
    pop rax
    add rsp, 8           ; Remove error code
    iretq

; Initialize memory management
init_memory:
    push rbp
    mov rbp, rsp
    
    ; Identity map first 2MB for startup
    mov rax, 0x200000    ; 2MB page
    or rax, PG_PRESENT | PG_WRITE | PG_PSE
    mov [pml4], rax
    
    ; Set up recursive page mapping
    mov rax, pml4
    or rax, PG_PRESENT | PG_WRITE
    mov [pml4 + 511 * 8], rax
    
    ; Load CR3 with PML4 address
    mov rax, pml4
    mov cr3, rax
    
    ; Enable NX bit if available
    mov eax, 0x80000001
    cpuid
    test edx, 1 << 20    ; NX bit
    jz .no_nx
    
    ; Set EFER.NXE
    mov ecx, 0xC0000080
    rdmsr
    or eax, 1 << 11      ; NXE bit
    wrmsr

.no_nx:
    pop rbp
    ret

; ============================================================================
; INTERRUPT HANDLING
; ============================================================================

init_idt:
    push rbp
    mov rbp, rsp
    
    ; Fill IDT with default handlers
    mov rdi, idt
    mov rcx, 256
    mov rax, default_interrupt_handler
    
.fill_idt:
    ; Set interrupt gate
    mov [rdi], ax           ; Handler low 16 bits
    mov [rdi + 2], cs       ; Code segment
    mov byte [rdi + 4], 0   ; IST (0 for now)
    mov byte [rdi + 5], 0x8E ; Present, DPL=0, 64-bit interrupt gate
    shr rax, 16
    mov [rdi + 6], ax       ; Handler high 16 bits
    shr rax, 16
    mov [rdi + 8], eax      ; Handler high 32 bits
    mov dword [rdi + 12], 0 ; Reserved
    add rdi, 16
    loop .fill_idt
    
    ; Set up specific handlers
    mov rdi, idt + 14 * 16  ; Page fault at vector 14
    mov rax, page_fault_handler
    call set_idt_entry
    
    mov rdi, idt + 0x80 * 16 ; System call at vector 0x80
    mov rax, syscall_handler
    call set_idt_entry
    
    ; Load IDT
    lidt [idt_descriptor]
    
    pop rbp
    ret

set_idt_entry:
    ; rdi = IDT entry, rax = handler
    mov [rdi], ax           ; Handler low 16 bits
    mov [rdi + 2], cs       ; Code segment
    mov byte [rdi + 4], 0   ; IST
    mov byte [rdi + 5], 0x8E ; Present, DPL=0, 64-bit interrupt gate
    shr rax, 16
    mov [rdi + 6], ax       ; Handler high 16 bits
    shr rax, 16
    mov [rdi + 8], eax      ; Handler high 32 bits
    mov dword [rdi + 12], 0 ; Reserved
    ret

default_interrupt_handler:
    ; Acknowledge interrupt
    mov al, 0x20
    out 0x20, al
    
    iretq

; ============================================================================
; UTILITY FUNCTIONS
; ============================================================================

kernel_print:
    ; rdi = string pointer
    push rbx
    mov rbx, rdi
    
.print_loop:
    mov al, [rbx]
    test al, al
    jz .print_done
    
    ; Print character (simplified - just store somewhere)
    ; In real kernel, would use console driver
    mov [console_buffer], al
    inc rbx
    jmp .print_loop

.print_done:
    pop rbx
    ret

get_current_task:
    mov rax, [current_task]
    ret

; ============================================================================
; DATA SECTION
; ============================================================================

section .data
align 4096

; Kernel boot message
kernel_boot_msg: db "Linux kernel booting...", 0

; Task list
task_list: dq 0
current_task: dq 0

; Page tables
align 4096
pml4: times 512 dq 0
pdpt: times 512 dq 0
pd:   times 512 dq 0

; IDT
align 16
idt: times 256 dq 0, 0  ; 256 entries, 16 bytes each
idt_descriptor:
    dw 256 * 16 - 1     ; Limit
    dq idt              ; Base

; System call table
syscall_table:
    dq sys_read         ; 0
    dq sys_write        ; 1
    dq sys_open         ; 2
    dq sys_close        ; 3
    dq sys_exit         ; 60
MAX_SYSCALLS equ ($ - syscall_table) / 8

; Console buffer
console_buffer: db 0

; ============================================================================
; STACK SECTION
; ============================================================================

section .bss
align 16

; Kernel stack
kernel_stack_bottom: resb 16384  ; 16KB kernel stack
kernel_stack_top:

; Task structures (simplified - would be allocated dynamically)
task0: resb task_struct.size
task1: resb task_struct.size
task2: resb task_struct.size
