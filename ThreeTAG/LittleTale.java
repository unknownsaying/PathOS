package com.littletale.mod;

import net.minecraftforge.fml.common.Mod;
import net.minecraftforge.fml.common.Mod.EventHandler;
import net.minecraftforge.fml.common.event.FMLInitializationEvent;
import net.minecraftforge.fml.common.event.FMLPostInitializationEvent;
import net.minecraftforge.fml.common.event.FMLPreInitializationEvent;
import net.minecraftforge.fml.common.registry.GameRegistry;
import net.minecraftforge.fml.relauncher.Side;
import net.minecraftforge.fml.relauncher.SideOnly;
import net.minecraftforge.common.MinecraftForge;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.block.model.ModelResourceLocation;
import net.minecraft.item.Item;
import net.minecraft.item.ItemBlock;
import net.minecraft.block.Block;
import net.minecraft.block.material.Material;
import net.minecraft.util.ResourceLocation;
import net.minecraft.util.SoundEvent;
import net.minecraftforge.client.model.ModelLoader;
import net.minecraftforge.event.RegistryEvent;
import net.minecraftforge.fml.common.eventhandler.SubscribeEvent;
import net.minecraftforge.fml.common.eventhandler.EventPriority;
import net.minecraftforge.fml.common.gameevent.PlayerEvent;
import net.minecraftforge.fml.common.gameevent.TickEvent;
import net.minecraft.entity.player.EntityPlayer;
import net.minecraft.world.World;
import net.minecraft.world.biome.Biome;
import net.minecraft.world.chunk.Chunk;
import net.minecraft.init.Blocks;
import net.minecraft.init.Items;
import net.minecraft.init.SoundEvents;
import net.minecraft.item.ItemStack;
import net.minecraft.item.crafting.IRecipe;
import net.minecraft.util.math.BlockPos;
import net.minecraft.util.text.TextComponentString;
import net.minecraft.util.text.TextFormatting;
import net.minecraft.village.Village;
import net.minecraft.entity.Entity;
import net.minecraft.entity.EntityCreature;
import net.minecraft.entity.EnumCreatureType;
import net.minecraft.entity.ai.EntityAITasks;
import net.minecraft.entity.monster.EntityZombie;
import net.minecraft.entity.passive.EntityVillager;
import net.minecraft.nbt.NBTTagCompound;
import net.minecraft.network.datasync.DataParameter;
import net.minecraft.network.datasync.DataSerializers;
import net.minecraft.network.datasync.EntityDataManager;
import org.apache.logging.log4j.Logger;

import java.util.*;
import java.util.concurrent.ThreadLocalRandom;

@Mod(modid = LittleTaleMod.MODID, 
     name = LittleTaleMod.NAME, 
     version = LittleTaleMod.VERSION,
     acceptedMinecraftVersions = "[1.12.2]")
public class LittleTaleMod {
    
    public static final String MODID = "littletale";
    public static final String NAME = "LittleTale Mod";
    public static final String VERSION = "1.0.0";
    
    public static Logger logger;
    
    // Mod instances
    public static Block STORY_TABLE_BLOCK;
    public static Item TALE_SCROLL;
    public static Item STORY_SHARD;
    public static Item MEMORY_FRAGMENT;
    
    // Entities
    public static Class STORYTELLER_ENTITY;
    
    // Config
    public static boolean ENABLE_RANDOM_EVENTS = true;
    public static int STORY_GENERATION_RADIUS = 100;
    public static float STORY_COMPLEXITY = 0.5f;
    
    // Story generation system
    private StoryGenerator storyGenerator;
    private WorldStoryManager worldStoryManager;
    
    @Mod.Instance
    public static LittleTaleMod instance;
    
    @EventHandler
    public void preInit(FMLPreInitializationEvent event) {
        logger = event.getModLog();
        
        // Initialize systems
        storyGenerator = new StoryGenerator();
        worldStoryManager = new WorldStoryManager();
        
        // Register blocks and items
        STORY_TABLE_BLOCK = new BlockStoryTable(Material.WOOD)
            .setRegistryName(MODID, "story_table")
            .setUnlocalizedName("story_table")
            .setCreativeTab(net.minecraft.creativetab.CreativeTabs.DECORATIONS);
        
        TALE_SCROLL = new ItemTaleScroll()
            .setRegistryName(MODID, "tale_scroll")
            .setUnlocalizedName("tale_scroll")
            .setCreativeTab(net.minecraft.creativetab.CreativeTabs.MISC);
        
        STORY_SHARD = new ItemStoryShard()
            .setRegistryName(MODID, "story_shard")
            .setUnlocalizedName("story_shard")
            .setCreativeTab(net.minecraft.creativetab.CreativeTabs.MISC);
        
        MEMORY_FRAGMENT = new ItemMemoryFragment()
            .setRegistryName(MODID, "memory_fragment")
            .setUnlocalizedName("memory_fragment")
            .setCreativeTab(net.minecraft.creativetab.CreativeTabs.MISC);
        
        // Register entities
        StoryTellerEntity.registerEntity();
        
        // Register event handlers
        MinecraftForge.EVENT_BUS.register(this);
        MinecraftForge.EVENT_BUS.register(new StoryEventHandler());
        MinecraftForge.EVENT_BUS.register(new WorldGenerationHandler());
        
        logger.info("LittleTale Mod pre-initialization complete!");
    }
    
    @EventHandler
    public void init(FMLInitializationEvent event) {
        // Register recipes
        registerRecipes();
        
        // Register world generators
        GameRegistry.registerWorldGenerator(new StoryWorldGenerator(), 10);
        
        // Register entity spawns
        registerEntitySpawns();
        
        if (event.getSide() == Side.CLIENT) {
            registerRenders();
        }
        
        logger.info("LittleTale Mod initialization complete!");
    }
    
    @EventHandler
    public void postInit(FMLPostInitializationEvent event) {
        // Cross-mod compatibility
        checkModCompat();
        
        // Finalize story systems
        storyGenerator.initializeThemes();
        worldStoryManager.loadStoryDatabase();
        
        logger.info("LittleTale Mod post-initialization complete!");
    }
    
    @SubscribeEvent
    public void registerBlocks(RegistryEvent.Register<Block> event) {
        event.getRegistry().register(STORY_TABLE_BLOCK);
    }
    
    @SubscribeEvent
    public void registerItems(RegistryEvent.Register<Item> event) {
        event.getRegistry().register(TALE_SCROLL);
        event.getRegistry().register(STORY_SHARD);
        event.getRegistry().register(MEMORY_FRAGMENT);
        event.getRegistry().register(new ItemBlock(STORY_TABLE_BLOCK)
            .setRegistryName(STORY_TABLE_BLOCK.getRegistryName()));
    }
    
    @SideOnly(Side.CLIENT)
    private void registerRenders() {
        // Register block renders
        ModelLoader.setCustomModelResourceLocation(
            Item.getItemFromBlock(STORY_TABLE_BLOCK), 0,
            new ModelResourceLocation(STORY_TABLE_BLOCK.getRegistryName(), "inventory"));
        
        // Register item renders
        ModelLoader.setCustomModelResourceLocation(TALE_SCROLL, 0,
            new ModelResourceLocation(TALE_SCROLL.getRegistryName(), "inventory"));
        ModelLoader.setCustomModelResourceLocation(STORY_SHARD, 0,
            new ModelResourceLocation(STORY_SHARD.getRegistryName(), "inventory"));
        ModelLoader.setCustomModelResourceLocation(MEMORY_FRAGMENT, 0,
            new ModelResourceLocation(MEMORY_FRAGMENT.getRegistryName(), "inventory"));
    }
    
    private void registerRecipes() {
        // Story Table crafting recipe
        GameRegistry.addShapedRecipe(
            new ResourceLocation(MODID, "story_table"),
            new ResourceLocation(MODID, "story_table"),
            new ItemStack(STORY_TABLE_BLOCK),
            "BBB",
            "BOB",
            "BBB",
            'B', Items.BOOK,
            'O', Blocks.OBSIDIAN
        );
        
        // Tale Scroll crafting
        GameRegistry.addShapelessRecipe(
            new ResourceLocation(MODID, "tale_scroll"),
            new ResourceLocation(MODID, "tale_scroll"),
            new ItemStack(TALE_SCROLL),
            Items.PAPER, Items.FEATHER, Items.INK_SAC
        );
    }
    
    private void registerEntitySpawns() {
        // Register StoryTeller spawn in villages
        net.minecraftforge.fml.common.registry.EntityRegistry.addSpawn(
            StoryTellerEntity.class,
            5,  // spawn weight
            1,  // min group
            1,  // max group
            EnumCreatureType.CREATURE,
            Biome.getBiome(1),  // Plains
            Biome.getBiome(4)   // Forest
        );
    }
    
    private void checkModCompat() {
        // Check for other mods and enable compatibility features
        try {
            Class.forName("vazkii.botania.common.Botania");
            logger.info("Botania detected! Enabling flower-based story generation...");
        } catch (ClassNotFoundException e) {
            // Botania not present
        }
        
        try {
            Class.forName("thaumcraft.Thaumcraft");
            logger.info("Thaumcraft detected! Enabling arcane story elements...");
        } catch (ClassNotFoundException e) {
            // Thaumcraft not present
        }
    }
    
    // Story Generator Core Class
    public static class StoryGenerator {
        private final Random random = new Random();
        private final List<StoryTheme> themes = new ArrayList<>();
        private final Map<String, List<String>> storyElements = new HashMap<>();
        
        public StoryGenerator() {
            initializeStoryElements();
        }
        
        private void initializeStoryElements() {
            // Characters
            storyElements.put("characters", Arrays.asList(
                "brave knight", "wise wizard", "cunning thief", "noble king",
                "mysterious wanderer", "ancient dragon", "lost prince", "forest spirit",
                "sea captain", "mountain hermit", "star traveler", "time mage"
            ));
            
            // Locations
            storyElements.put("locations", Arrays.asList(
                "ancient castle", "enchanted forest", "forgotten temple",
                "floating island", "underwater city", "crystal cavern",
                "cloud kingdom", "lava fortress", "ice palace", "desert oasis"
            ));
            
            // Objects
            storyElements.put("objects", Arrays.asList(
                "crystal orb", "enchanted sword", "lost crown", "time hourglass",
                "star map", "dragon egg", "phoenix feather", "moon pearl",
                "sun stone", "void key", "memory mirror", "dream catcher"
            ));
            
            // Conflicts
            storyElements.put("conflicts", Arrays.asList(
                "must retrieve the stolen artifact",
                "needs to break an ancient curse",
                "must save their kingdom from invasion",
                "seeks to uncover a hidden truth",
                "must prevent a magical catastrophe",
                "needs to find their lost memory",
                "must stop a dimensional rift",
                "seeks to restore balance to the elements"
            ));
            
            // Endings
            storyElements.put("endings", Arrays.asList(
                "found peace and prosperity",
                "discovered a greater purpose",
                "sacrificed themselves for others",
                "became a legend remembered for ages",
                "vanished into the mists of time",
                "found a new home and family",
                "gained wisdom but lost innocence",
                "changed the world forever"
            ));
        }
        
        public void initializeThemes() {
            themes.add(new StoryTheme("heroic", 0.3f, 
                Arrays.asList("characters", "conflicts", "endings")));
            themes.add(new StoryTheme("mystery", 0.25f,
                Arrays.asList("locations", "objects", "conflicts")));
            themes.add(new StoryTheme("fantasy", 0.35f,
                Arrays.asList("characters", "locations", "objects")));
            themes.add(new StoryTheme("tragic", 0.1f,
                Arrays.asList("conflicts", "endings")));
        }
        
        public GeneratedStory generateStory(World world, BlockPos location) {
            StoryTheme theme = selectTheme(world, location);
            GeneratedStory story = new GeneratedStory();
            
            story.theme = theme.name;
            story.title = generateTitle(theme);
            story.content = generateContent(theme);
            story.locationHint = generateLocationHint(world, location);
            story.rewards = generateRewards(theme);
            story.difficulty = calculateDifficulty(world, location);
            
            // Add story-specific effects
            if (theme.name.equals("fantasy")) {
                story.effects.add("enchanted");
            } else if (theme.name.equals("tragic")) {
                story.effects.add("melancholy");
            }
            
            return story;
        }
        
        private StoryTheme selectTheme(World world, BlockPos pos) {
            // Biome-based theme selection
            Biome biome = world.getBiome(pos);
            String biomeName = biome.getBiomeName().toLowerCase();
            
            if (biomeName.contains("forest") || biomeName.contains("jungle")) {
                return getTheme("fantasy");
            } else if (biomeName.contains("desert") || biomeName.contains("mesa")) {
                return getTheme("mystery");
            } else if (biomeName.contains("extreme") || biomeName.contains("icy")) {
                return getTheme("tragic");
            }
            
            // Default to weighted random
            float roll = random.nextFloat();
            float cumulative = 0;
            
            for (StoryTheme theme : themes) {
                cumulative += theme.weight;
                if (roll <= cumulative) {
                    return theme;
                }
            }
            
            return themes.get(0);
        }
        
        private String generateTitle(StoryTheme theme) {
            String template = "";
            switch (theme.name) {
                case "heroic": template = "The %s of the %s"; break;
                case "mystery": template = "Secrets of the %s"; break;
                case "fantasy": template = "%s and the %s"; break;
                case "tragic": template = "The Last %s"; break;
            }
            
            String element1 = getRandomElement("characters");
            String element2 = getRandomElement("locations");
            
            return String.format(template, element1, element2);
        }
        
        private String generateContent(StoryTheme theme) {
            StringBuilder content = new StringBuilder();
            
            content.append("Once upon a time, there was a ")
                   .append(getRandomElement("characters"))
                   .append(" who lived in a ")
                   .append(getRandomElement("locations"))
                   .append(".\n\n");
            
            content.append("One day, they discovered that they ")
                   .append(getRandomElement("conflicts"))
                   .append(".\n\n");
            
            content.append("Their journey led them through many trials, ")
                   .append("searching for the legendary ")
                   .append(getRandomElement("objects"))
                   .append(".\n\n");
            
            content.append("In the end, they ")
                   .append(getRandomElement("endings"))
                   .append(".");
            
            return content.toString();
        }
        
        private String generateLocationHint(World world, BlockPos center) {
            int radius = STORY_GENERATION_RADIUS;
            int x = center.getX() + random.nextInt(radius * 2) - radius;
            int z = center.getZ() + random.nextInt(radius * 2) - radius;
            int y = world.getTopSolidOrLiquidBlock(new BlockPos(x, 0, z)).getY();
            
            return String.format("Look near coordinates: %d, %d, %d", x, y, z);
        }
        
        private List<ItemStack> generateRewards(StoryTheme theme) {
            List<ItemStack> rewards = new ArrayList<>();
            
            // Base rewards
            rewards.add(new ItemStack(TALE_SCROLL, 1 + random.nextInt(3)));
            rewards.add(new ItemStack(STORY_SHARD, 2 + random.nextInt(5)));
            
            // Theme-specific rewards
            switch (theme.name) {
                case "heroic":
                    rewards.add(new ItemStack(Items.DIAMOND, 1));
                    rewards.add(new ItemStack(Items.GOLDEN_APPLE, 1));
                    break;
                case "mystery":
                    rewards.add(new ItemStack(Items.ENDER_PEARL, 1 + random.nextInt(2)));
                    rewards.add(new ItemStack(Items.COMPASS, 1));
                    break;
                case "fantasy":
                    rewards.add(new ItemStack(Items.GHAST_TEAR, 1));
                    rewards.add(new ItemStack(Items.PRISMARINE_CRYSTALS, 2 + random.nextInt(3)));
                    break;
                case "tragic":
                    rewards.add(new ItemStack(Items.EMERALD, 2 + random.nextInt(3)));
                    rewards.add(new ItemStack(MEMORY_FRAGMENT, 1));
                    break;
            }
            
            return rewards;
        }
        
        private int calculateDifficulty(World world, BlockPos pos) {
            int difficulty = 1;
            
            // Time-based difficulty (night is harder)
            long time = world.getWorldTime() % 24000;
            if (time > 13000 && time < 23000) {
                difficulty += 2;
            }
            
            // Location-based difficulty
            if (world.getBiome(pos).isHighHumidity()) {
                difficulty += 1;
            }
            if (world.getDifficulty().getDifficultyId() > 1) {
                difficulty += world.getDifficulty().getDifficultyId();
            }
            
            return Math.min(difficulty, 10);
        }
        
        private String getRandomElement(String category) {
            List<String> elements = storyElements.get(category);
            return elements.get(random.nextInt(elements.size()));
        }
        
        private StoryTheme getTheme(String name) {
            return themes.stream()
                .filter(t -> t.name.equals(name))
                .findFirst()
                .orElse(themes.get(0));
        }
        
        private class StoryTheme {
            String name;
            float weight;
            List<String> elementCategories;
            
            StoryTheme(String name, float weight, List<String> elementCategories) {
                this.name = name;
                this.weight = weight;
                this.elementCategories = elementCategories;
            }
        }
    }
    
    // Generated Story Data Class
    public static class GeneratedStory {
        public String title;
        public String content;
        public String theme;
        public String locationHint;
        public List<ItemStack> rewards;
        public List<String> effects = new ArrayList<>();
        public int difficulty;
        public long generationTime;
        public UUID storyId;
        
        public GeneratedStory() {
            this.storyId = UUID.randomUUID();
            this.generationTime = System.currentTimeMillis();
        }
        
        public NBTTagCompound writeToNBT() {
            NBTTagCompound compound = new NBTTagCompound();
            compound.setString("title", title);
            compound.setString("content", content);
            compound.setString("theme", theme);
            compound.setString("locationHint", locationHint);
            compound.setInteger("difficulty", difficulty);
            compound.setUniqueId("storyId", storyId);
            compound.setLong("generationTime", generationTime);
            
            // Write effects
            NBTTagCompound effectsTag = new NBTTagCompound();
            for (int i = 0; i < effects.size(); i++) {
                effectsTag.setString("effect_" + i, effects.get(i));
            }
            compound.setTag("effects", effectsTag);
            
            return compound;
        }
        
        public void readFromNBT(NBTTagCompound compound) {
            title = compound.getString("title");
            content = compound.getString("content");
            theme = compound.getString("theme");
            locationHint = compound.getString("locationHint");
            difficulty = compound.getInteger("difficulty");
            storyId = compound.getUniqueId("storyId");
            generationTime = compound.getLong("generationTime");
            
            // Read effects
            NBTTagCompound effectsTag = compound.getCompoundTag("effects");
            effects.clear();
            for (int i = 0; effectsTag.hasKey("effect_" + i); i++) {
                effects.add(effectsTag.getString("effect_" + i));
            }
        }
    }
    
    // World Story Manager
    public static class WorldStoryManager {
        private final Map<UUID, GeneratedStory> playerStories = new HashMap<>();
        private final Map<Long, GeneratedStory> chunkStories = new HashMap<>(); // Chunk key -> Story
        
        public void loadStoryDatabase() {
            // Load saved stories from world data
            // Implementation would read from file
        }
        
        public void saveStoryDatabase() {
            // Save stories to world data
        }
        
        public void assignStoryToChunk(World world, Chunk chunk, GeneratedStory story) {
            long chunkKey = getChunkKey(chunk.x, chunk.z);
            chunkStories.put(chunkKey, story);
            
            // Mark chunk with story metadata
            NBTTagCompound chunkData = chunk.getTileEntityMap()
                .computeIfAbsent(new BlockPos(chunk.x << 4, 64, chunk.z << 4), 
                               k -> new NBTTagCompound());
            chunkData.setBoolean("hasStory", true);
            chunkData.setUniqueId("storyId", story.storyId);
        }
        
        public GeneratedStory getStoryForChunk(World world, int chunkX, int chunkZ) {
            long chunkKey = getChunkKey(chunkX, chunkZ);
            return chunkStories.get(chunkKey);
        }
        
        public void completeStory(EntityPlayer player, GeneratedStory story) {
            playerStories.put(player.getUniqueID(), story);
            
            // Grant rewards
            for (ItemStack reward : story.rewards) {
                if (!player.inventory.addItemStackToInventory(reward.copy())) {
                    player.dropItem(reward.copy(), false);
                }
            }
            
            // Apply effects
            for (String effect : story.effects) {
                applyStoryEffect(player, effect);
            }
            
            // Experience reward based on difficulty
            player.addExperience(10 * story.difficulty);
            
            // Send completion message
            player.sendMessage(new TextComponentString(
                TextFormatting.GOLD + "Story Completed: " + story.title + 
                TextFormatting.GREEN + " (+" + (10 * story.difficulty) + " XP)"));
        }
        
        private void applyStoryEffect(EntityPlayer player, String effect) {
            switch (effect) {
                case "enchanted":
                    // Temporary potion effect
                    player.addPotionEffect(new net.minecraft.potion.PotionEffect(
                        net.minecraft.init.MobEffects.LUCK, 12000, 0));
                    break;
                case "melancholy":
                    // Add memory fragment
                    player.inventory.addItemStackToInventory(new ItemStack(MEMORY_FRAGMENT));
                    break;
            }
        }
        
        private long getChunkKey(int x, int z) {
            return ((long) x << 32) | (z & 0xFFFFFFFFL);
        }
    }
}

// Custom Block: Story Table
class BlockStoryTable extends Block {
    public BlockStoryTable(Material material) {
        super(material);
        setHardness(2.0f);
        setResistance(10.0f);
        setLightLevel(0.5f);
        setHarvestLevel("axe", 0);
    }
    
    @Override
    public boolean onBlockActivated(World world, BlockPos pos, 
                                   net.minecraft.block.state.IBlockState state,
                                   EntityPlayer player, 
                                   net.minecraft.util.EnumHand hand,
                                   net.minecraft.util.EnumFacing facing,
                                   float hitX, float hitY, float hitZ) {
        
        if (!world.isRemote && hand == net.minecraft.util.EnumHand.MAIN_HAND) {
            // Generate or retrieve story
            LittleTaleMod.StoryGenerator generator = LittleTaleMod.instance.storyGenerator;
            LittleTaleMod.GeneratedStory story = generator.generateStory(world, pos);
            
            // Display story to player
            player.sendMessage(new TextComponentString(
                TextFormatting.DARK_PURPLE + "=== " + story.title + " ==="));
            player.sendMessage(new TextComponentString(
                TextFormatting.WHITE + story.content));
            player.sendMessage(new TextComponentString(
                TextFormatting.YELLOW + "Hint: " + story.locationHint));
            player.sendMessage(new TextComponentString(
                TextFormatting.GREEN + "Difficulty: " + 
                TextFormatting.RED + "★".repeat(story.difficulty)));
            
            // Store story for completion
            LittleTaleMod.instance.worldStoryManager.assignStoryToChunk(
                world, world.getChunkFromBlockCoords(pos), story);
            
            return true;
        }
        
        return super.onBlockActivated(world, pos, state, player, hand, facing, hitX, hitY, hitZ);
    }
}

// Custom Item: Tale Scroll
class ItemTaleScroll extends Item {
    public ItemTaleScroll() {
        setMaxStackSize(16);
        setCreativeTab(net.minecraft.creativetab.CreativeTabs.MISC);
    }
    
    @Override
    public net.minecraft.util.ActionResult<ItemStack> onItemRightClick(World world, 
                                                                      EntityPlayer player,
                                                                      net.minecraft.util.EnumHand hand) {
        ItemStack stack = player.getHeldItem(hand);
        
        if (!world.isRemote) {
            // Read a random story excerpt
            List<String> excerpts = Arrays.asList(
                "In a land far away...",
                "The old legends speak of...",
                "Beneath the ancient moon...",
                "Whispers in the wind tell of...",
                "Long ago, before memory began..."
            );
            
            String excerpt = excerpts.get(world.rand.nextInt(excerpts.size()));
            player.sendMessage(new TextComponentString(
                TextFormatting.ITALIC + excerpt));
            
            // Chance to reveal a clue
            if (world.rand.nextFloat() < 0.2f) {
                BlockPos nearestStructure = world.findNearestStructure(
                    "Stronghold", player.getPosition(), false);
                if (nearestStructure != null) {
                    player.sendMessage(new TextComponentString(
                        TextFormatting.GOLD + "The scroll reveals ancient writing " +
                        "pointing towards coordinates..."));
                }
            }
            
            // Consume scroll
            stack.shrink(1);
        }
        
        return super.onItemRightClick(world, player, hand);
    }
}

// Custom Item: Story Shard
class ItemStoryShard extends Item {
    public ItemStoryShard() {
        setMaxStackSize(64);
        setCreativeTab(net.minecraft.creativetab.CreativeTabs.MISC);
    }
    
    @Override
    public boolean hasEffect(ItemStack stack) {
        return true; // Always enchanted look
    }
}

// Custom Item: Memory Fragment
class ItemMemoryFragment extends Item {
    public ItemMemoryFragment() {
        setMaxStackSize(1);
        setCreativeTab(net.minecraft.creativetab.CreativeTabs.MISC);
    }
    
    @Override
    public net.minecraft.util.ActionResult<ItemStack> onItemRightClick(World world, 
                                                                      EntityPlayer player,
                                                                      net.minecraft.util.EnumHand hand) {
        ItemStack stack = player.getHeldItem(hand);
        
        if (!world.isRemote) {
            // Restore player memory (experience)
            int xpAmount = 50 + world.rand.nextInt(100);
            player.addExperience(xpAmount);
            
            // Random memory flashback
            String[] memories = {
                "You remember a childhood in a village...",
                "A forgotten spell comes to mind...",
                "The memory of an ancient battle surfaces...",
                "You recall a secret path through the mountains...",
                "A lost language suddenly makes sense..."
            };
            
            player.sendMessage(new TextComponentString(
                TextFormatting.AQUA + memories[world.rand.nextInt(memories.length)]));
            player.sendMessage(new TextComponentString(
                TextFormatting.GREEN + "Gained " + xpAmount + " experience from the memory!"));
            
            // Consume fragment
            stack.shrink(1);
        }
        
        return super.onItemRightClick(world, player, hand);
    }
}

// StoryTeller Entity
class StoryTellerEntity extends EntityVillager {
    private static final DataParameter<String> STORY_TYPE = 
        EntityDataManager.createKey(StoryTellerEntity.class, DataSerializers.STRING);
    private static final DataParameter<Integer> STORY_COUNT = 
        EntityDataManager.createKey(StoryTellerEntity.class, DataSerializers.VARINT);
    
    private List<String> availableStories = new ArrayList<>();
    private long lastStoryTime = 0;
    
    public StoryTellerEntity(World world) {
        super(world);
        this.setSize(0.6F, 1.95F);
        this.enablePersistence();
    }
    
    public StoryTellerEntity(World world, int professionId) {
        super(world, professionId);
        this.setSize(0.6F, 1.95F);
    }
    
    public static void registerEntity() {
        net.minecraftforge.fml.common.registry.EntityRegistry.registerModEntity(
            new ResourceLocation(LittleTaleMod.MODID, "storyteller"),
            StoryTellerEntity.class, "storyteller", 0,
            LittleTaleMod.instance, 64, 3, true,
            0x996633, 0x663300);
    }
    
    @Override
    protected void entityInit() {
        super.entityInit();
        this.dataManager.register(STORY_TYPE, "unknown");
        this.dataManager.register(STORY_COUNT, 0);
    }
    
    @Override
    protected void initEntityAI() {
        super.initEntityAI();
        
        // Custom AI tasks
        this.tasks.addTask(1, new EntityAITellStory(this));
        this.tasks.addTask(2, new EntityAICollectStories(this));
        
        // Remove some default villager AI
        this.tasks.taskEntries.removeIf(task -> 
            task.action instanceof net.minecraft.entity.ai.EntityAIAvoidEntity);
    }
    
    @Override
    public boolean processInteract(EntityPlayer player, net.minecraft.util.EnumHand hand) {
        if (!this.world.isRemote && hand == net.minecraft.util.EnumHand.MAIN_HAND) {
            // Tell a story to the player
            if (availableStories.isEmpty()) {
                generateNewStories();
            }
            
            if (!availableStories.isEmpty()) {
                String story = availableStories.remove(0);
                player.sendMessage(new TextComponentString(
                    TextFormatting.DARK_GREEN + "Storyteller says: " + 
                    TextFormatting.WHITE + story));
                
                // Update story count
                int count = this.dataManager.get(STORY_COUNT);
                this.dataManager.set(STORY_COUNT, count + 1);
                
                // Reward player
                if (world.rand.nextFloat() < 0.3f) {
                    player.addItemStackToInventory(new ItemStack(LittleTaleMod.TALE_SCROLL));
                }
                
                // Cooldown
                lastStoryTime = world.getTotalWorldTime();
                
                return true;
            }
        }
        
        return super.processInteract(player, hand);
    }
    
    private void generateNewStories() {
        availableStories.clear();
        
        String[] storyTemplates = {
            "I once met a traveler who spoke of %s near the %s.",
            "Legend tells of %s who sought the %s in the %s.",
            "Have you heard the tale of %s and the %s? It happened at %s.",
            "My grandmother told me about %s who discovered %s in the %s."
        };
        
        LittleTaleMod.StoryGenerator generator = LittleTaleMod.instance.storyGenerator;
        
        for (int i = 0; i < 3 + world.rand.nextInt(3); i++) {
            String template = storyTemplates[world.rand.nextInt(storyTemplates.length)];
            String story = String.format(template,
                generator.getRandomElement("characters"),
                generator.getRandomElement("objects"),
                generator.getRandomElement("locations"));
            availableStories.add(story);
        }
    }
    
    @Override
    protected void updateAITasks() {
        super.updateAITasks();
        
        // Generate new stories periodically
        if (world.getTotalWorldTime() - lastStoryTime > 24000 && availableStories.size() < 3) {
            generateNewStories();
        }
    }
    
    @Override
    public void writeEntityToNBT(NBTTagCompound compound) {
        super.writeEntityToNBT(compound);
        compound.setString("StoryType", this.dataManager.get(STORY_TYPE));
        compound.setInteger("StoryCount", this.dataManager.get(STORY_COUNT));
        
        // Save available stories
        NBTTagCompound storiesTag = new NBTTagCompound();
        for (int i = 0; i < availableStories.size(); i++) {
            storiesTag.setString("story_" + i, availableStories.get(i));
        }
        compound.setTag("AvailableStories", storiesTag);
    }
    
    @Override
    public void readEntityFromNBT(NBTTagCompound compound) {
        super.readEntityFromNBT(compound);
        this.dataManager.set(STORY_TYPE, compound.getString("StoryType"));
        this.dataManager.set(STORY_COUNT, compound.getInteger("StoryCount"));
        
        // Load available stories
        availableStories.clear();
        NBTTagCompound storiesTag = compound.getCompoundTag("AvailableStories");
        for (int i = 0; storiesTag.hasKey("story_" + i); i++) {
            availableStories.add(storiesTag.getString("story_" + i));
        }
    }
    
    // Custom AI Tasks
    static class EntityAITellStory extends net.minecraft.entity.ai.EntityAIBase {
        private final StoryTellerEntity storyteller;
        private EntityPlayer listeningPlayer;
        private int tellTime;
        
        public EntityAITellStory(StoryTellerEntity storyteller) {
            this.storyteller = storyteller;
            this.setMutexBits(3);
        }
        
        @Override
        public boolean shouldExecute() {
            this.listeningPlayer = this.storyteller.world.getClosestPlayerToEntity(
                this.storyteller, 5.0);
            return this.listeningPlayer != null && 
                   this.storyteller.availableStories.size() > 0;
        }
        
        @Override
        public void startExecuting() {
            this.tellTime = 100 + this.storyteller.world.rand.nextInt(100);
        }
        
        @Override
        public boolean shouldContinueExecuting() {
            return this.listeningPlayer != null && 
                   this.tellTime > 0 && 
                   this.storyteller.getDistanceSq(this.listeningPlayer) < 25.0;
        }
        
        @Override
        public void resetTask() {
            this.listeningPlayer = null;
            this.tellTime = 0;
        }
        
        @Override
        public void updateTask() {
            this.tellTime--;
            
            // Face the player
            this.storyteller.getLookHelper().setLookPositionWithEntity(
                this.listeningPlayer, 10.0F, (float)this.storyteller.getVerticalFaceSpeed());
            
            // Occasionally tell a story
            if (this.tellTime % 40 == 0 && !this.storyteller.availableStories.isEmpty()) {
                String story = this.storyteller.availableStories.get(0);
                this.listeningPlayer.sendMessage(new TextComponentString(
                    TextFormatting.DARK_GREEN + "Storyteller: " + 
                    TextFormatting.WHITE + story));
                
                // Remove told story
                this.storyteller.availableStories.remove(0);
            }
        }
    }
    
    static class EntityAICollectStories extends net.minecraft.entity.ai.EntityAIBase {
        private final StoryTellerEntity storyteller;
        private BlockPos targetPos;
        private int searchCooldown;
        
        public EntityAICollectStories(StoryTellerEntity storyteller) {
            this.storyteller = storyteller;
            this.setMutexBits(1);
        }
        
        @Override
        public boolean shouldExecute() {
            if (this.searchCooldown > 0) {
                this.searchCooldown--;
                return false;
            }
            
            // Look for story-related blocks
            if (this.storyteller.availableStories.size() < 5) {
                this.targetPos = findStorySource();
                return this.targetPos != null;
            }
            
            return false;
        }
        
        @Override
        public boolean shouldContinueExecuting() {
            return this.targetPos != null && 
                   this.storyteller.getDistanceSq(this.targetPos) > 1.0;
        }
        
        @Override
        public void startExecuting() {
            this.storyteller.getNavigator().tryMoveToXYZ(
                this.targetPos.getX(), this.targetPos.getY(), this.targetPos.getZ(), 0.6);
        }
        
        @Override
        public void resetTask() {
            this.targetPos = null;
            this.searchCooldown = 200 + this.storyteller.world.rand.nextInt(200);
        }
        
        @Override
        public void updateTask() {
            if (this.storyteller.getDistanceSq(this.targetPos) <= 2.25) {
                // Found a story source
                this.storyteller.generateNewStories();
                this.resetTask();
            }
        }
        
        private BlockPos findStorySource() {
            World world = this.storyteller.world;
            BlockPos entityPos = this.storyteller.getPosition();
            
            // Look for bookshelves, lecterns, or our story table
            for (int i = 0; i < 10; i++) {
                int x = entityPos.getX() + world.rand.nextInt(32) - 16;
                int z = entityPos.getZ() + world.rand.nextInt(32) - 16;
                int y = world.getTopSolidOrLiquidBlock(new BlockPos(x, 0, z)).getY();
                
                BlockPos checkPos = new BlockPos(x, y, z);
                net.minecraft.block.Block block = world.getBlockState(checkPos).getBlock();
                
                if (block == Blocks.BOOKSHELF || 
                    block == LittleTaleMod.STORY_TABLE_BLOCK ||
                    block == Blocks.LECTERN) {
                    return checkPos;
                }
            }
            
            return null;
        }
    }
}

// Event Handler for Story Events
class StoryEventHandler {
    @SubscribeEvent(priority = EventPriority.NORMAL)
    public void onPlayerJoin(PlayerEvent.PlayerLoggedInEvent event) {
        EntityPlayer player = event.player;
        player.sendMessage(new TextComponentString(
            TextFormatting.DARK_PURPLE + "Welcome to a world of stories!"));
        player.sendMessage(new TextComponentString(
            TextFormatting.GOLD + "Find Storytellers in villages or craft a Story Table to begin your tales."));
    }
    
    @SubscribeEvent
    public void onPlayerTick(TickEvent.PlayerTickEvent event) {
        if (event.phase == TickEvent.Phase.END && !event.player.world.isRemote) {
            // Random story events during gameplay
            if (event.player.world.rand.nextFloat() < 0.0001f) { // Very rare
                triggerRandomStoryEvent(event.player);
            }
        }
    }
    
    private void triggerRandomStoryEvent(EntityPlayer player) {
        String[] events = {
            "You feel a strange presence nearby, as if a story is waiting to be told...",
            "The wind carries whispers of ancient tales from the north...",
            "A mysterious figure watches you from a distance, then vanishes...",
            "You find an old, weathered scroll on the ground...",
            "Memories of a forgotten legend suddenly flood your mind..."
        };
        
        player.sendMessage(new TextComponentString(
            TextFormatting.ITALIC + events[player.world.rand.nextInt(events.length)]));
        
        // Small chance to spawn a story item
        if (player.world.rand.nextFloat() < 0.3f) {
            player.dropItem(new ItemStack(LittleTaleMod.STORY_SHARD), false);
        }
    }
}

// World Generation Handler
class WorldGenerationHandler {
    @SubscribeEvent
    public void onWorldLoad(net.minecraftforge.event.world.WorldEvent.Load event) {
        if (!event.getWorld().isRemote) {
            // Initialize story generation for this world
            World world = (World) event.getWorld();
            LittleTaleMod.instance.worldStoryManager.loadStoryDatabase();
        }
    }
    
    @SubscribeEvent
    public void onWorldSave(net.minecraftforge.event.world.WorldEvent.Save event) {
        if (!event.getWorld().isRemote) {
            // Save story data
            LittleTaleMod.instance.worldStoryManager.saveStoryDatabase();
        }
    }
}

// Story World Generator
class StoryWorldGenerator implements net.minecraft.world.gen.IChunkGenerator {
    private final Random random = new Random();
    
    @Override
    public Chunk generateChunk(int x, int z) {
        // In a real implementation, this would generate chunks
        // For now, we'll use it to mark chunks for story generation
        return null;
    }
    
    @Override
    public void populate(int x, int z) {
        // Populate chunk with story elements
        World world = net.minecraftforge.common.DimensionManager.getWorld(0);
        if (world != null) {
            // Chance to generate story structures
            if (random.nextFloat() < 0.01f) { // 1% chance per chunk
                generateStoryStructure(world, x, z);
            }
        }
    }
    
    @Override
    public boolean generateStructures(Chunk chunk, int x, int z) {
        return false;
    }
    
    @Override
    public List<net.minecraft.world.biome.Biome.SpawnListEntry> getPossibleCreatures(
        EnumCreatureType creatureType, BlockPos pos) {
        return Collections.emptyList();
    }
    
    @Override
    public BlockPos getNearestStructurePos(World world, String structureName, 
                                          BlockPos position, boolean findUnexplored) {
        return null;
    }
    
    @Override
    public void recreateStructures(Chunk chunk, int x, int z) {
    }
    
    @Override
    public boolean isInsideStructure(World world, String structureName, BlockPos pos) {
        return false;
    }
    
    private void generateStoryStructure(World world, int chunkX, int chunkZ) {
        int x = chunkX * 16 + random.nextInt(16);
        int z = chunkZ * 16 + random.nextInt(16);
        int y = world.getHeight(x, z);
        
        // Generate a small story monument
        BlockPos center = new BlockPos(x, y, z);
        
        // Stone platform
        for (int dx = -2; dx <= 2; dx++) {
            for (int dz = -2; dz <= 2; dz++) {
                world.setBlockState(center.add(dx, -1, dz), 
                    Blocks.STONE.getDefaultState());
            }
        }
        
        // Story Table at center
        world.setBlockState(center, 
            LittleTaleMod.STORY_TABLE_BLOCK.getDefaultState());
        
        // Decorative bookshelves
        world.setBlockState(center.add(2, 0, 0), 
            Blocks.BOOKSHELF.getDefaultState());
        world.setBlockState(center.add(-2, 0, 0), 
            Blocks.BOOKSHELF.getDefaultState());
        world.setBlockState(center.add(0, 0, 2), 
            Blocks.BOOKSHELF.getDefaultState());
        world.setBlockState(center.add(0, 0, -2), 
            Blocks.BOOKSHELF.getDefaultState());
        
        // Generate a story for this structure
        LittleTaleMod.StoryGenerator generator = LittleTaleMod.instance.storyGenerator;
        LittleTaleMod.GeneratedStory story = generator.generateStory(world, center);
        
        // Store story with chunk
        Chunk chunk = world.getChunkFromBlockCoords(center);
        LittleTaleMod.instance.worldStoryManager.assignStoryToChunk(world, chunk, story);
        
        // Place loot chest with story items
        BlockPos chestPos = center.add(0, 0, 3);
        world.setBlockState(chestPos, Blocks.CHEST.getDefaultState());
        
        net.minecraft.tileentity.TileEntityChest chest = 
            (net.minecraft.tileentity.TileEntityChest) world.getTileEntity(chestPos);
        if (chest != null) {
            // Add story rewards to chest
            for (ItemStack reward : story.rewards) {
                int slot = random.nextInt(chest.getSizeInventory());
                chest.setInventorySlotContents(slot, reward);
            }
            
            // Always add a tale scroll
            chest.setInventorySlotContents(0, new ItemStack(LittleTaleMod.TALE_SCROLL));
        }
    }
}
