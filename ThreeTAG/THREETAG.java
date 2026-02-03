package com.threetag.mod;

import net.minecraftforge.fml.common.Mod;
import net.minecraftforge.fml.common.Mod.EventHandler;
import net.minecraftforge.fml.common.event.FMLInitializationEvent;
import net.minecraftforge.fml.common.event.FMLPostInitializationEvent;
import net.minecraftforge.fml.common.event.FMLPreInitializationEvent;
import net.minecraftforge.fml.common.eventhandler.SubscribeEvent;
import net.minecraftforge.fml.common.gameevent.InputEvent;
import net.minecraftforge.fml.common.gameevent.TickEvent;
import net.minecraftforge.fml.relauncher.Side;
import net.minecraftforge.fml.relauncher.SideOnly;
import net.minecraftforge.client.settings.KeyConflictContext;
import net.minecraftforge.common.MinecraftForge;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiScreen;
import net.minecraft.client.gui.ScaledResolution;
import net.minecraft.client.gui.inventory.GuiContainer;
import net.minecraft.client.renderer.GlStateManager;
import net.minecraft.client.renderer.RenderHelper;
import net.minecraft.client.settings.KeyBinding;
import net.minecraft.entity.player.EntityPlayer;
import net.minecraft.entity.player.InventoryPlayer;
import net.minecraft.inventory.Container;
import net.minecraft.inventory.Slot;
import net.minecraft.item.Item;
import net.minecraft.item.ItemStack;
import net.minecraft.nbt.NBTTagCompound;
import net.minecraft.nbt.NBTTagList;
import net.minecraft.util.ResourceLocation;
import net.minecraft.util.text.TextComponentString;
import net.minecraft.util.text.TextFormatting;
import net.minecraft.world.World;
import net.minecraftforge.items.CapabilityItemHandler;
import net.minecraftforge.items.IItemHandler;
import org.lwjgl.input.Keyboard;
import org.lwjgl.opengl.GL11;

import java.util.*;
import java.util.stream.Collectors;

@Mod(modid = THREETAGMod.MODID, 
     name = THREETAGMod.NAME, 
     version = THREETAGMod.VERSION,
     acceptedMinecraftVersions = "[1.12.2]")
public class THREETAGMod {
    
    public static final String MODID = "threetag";
    public static final String NAME = "THREETAG Mod";
    public static final String VERSION = "1.0.0";
    
    // Key bindings
    @SideOnly(Side.CLIENT)
    public static KeyBinding openTagGui;
    @SideOnly(Side.CLIENT)
    public static KeyBinding quickTag1, quickTag2, quickTag3;
    @SideOnly(Side.CLIENT)
    public static KeyBinding tagSearchMode;
    
    // Tag system
    public static final Map<String, TagCategory> TAG_CATEGORIES = new HashMap<>();
    public static final List<TagPreset> TAG_PRESETS = new ArrayList<>();
    
    // Colors for tags (RGB)
    public static final int[] TAG_COLORS = {
        0xFF3366, 0x33CC66, 0x3366FF, 0xFF33CC, 0x33FFFF,
        0xFFFF33, 0xFF6633, 0x9966FF, 0x66FF99, 0xFF9933
    };
    
    @Mod.Instance
    public static THREETAGMod instance;
    
    @EventHandler
    public void preInit(FMLPreInitializationEvent event) {
        // Initialize tag system
        initializeTagCategories();
        initializeTagPresets();
        
        // Register event handlers
        MinecraftForge.EVENT_BUS.register(this);
        MinecraftForge.EVENT_BUS.register(new TagEventHandler());
        
        if (event.getSide() == Side.CLIENT) {
            registerKeyBindings();
        }
    }
    
    @EventHandler
    public void init(FMLInitializationEvent event) {
        // Register network handlers
        registerNetwork();
        
        // Register capabilities
        registerCapabilities();
    }
    
    @EventHandler
    public void postInit(FMLPostInitializationEvent event) {
        // Scan all registered items and assign default tags
        scanAllItemsForTags();
    }
    
    @SideOnly(Side.CLIENT)
    private void registerKeyBindings() {
        openTagGui = new KeyBinding("key.threetag.open", 
            Keyboard.KEY_T, "key.categories.threetag");
        quickTag1 = new KeyBinding("key.threetag.tag1", 
            Keyboard.KEY_NUMPAD1, "key.categories.threetag");
        quickTag2 = new KeyBinding("key.threetag.tag2", 
            Keyboard.KEY_NUMPAD2, "key.categories.threetag");
        quickTag3 = new KeyBinding("key.threetag.tag3", 
            Keyboard.KEY_NUMPAD3, "key.categories.threetag");
        tagSearchMode = new KeyBinding("key.threetag.search", 
            Keyboard.KEY_B, "key.categories.threetag");
        
        net.minecraftforge.fml.client.registry.ClientRegistry.registerKeyBinding(openTagGui);
        net.minecraftforge.fml.client.registry.ClientRegistry.registerKeyBinding(quickTag1);
        net.minecraftforge.fml.client.registry.ClientRegistry.registerKeyBinding(quickTag2);
        net.minecraftforge.fml.client.registry.ClientRegistry.registerKeyBinding(quickTag3);
        net.minecraftforge.fml.client.registry.ClientRegistry.registerKeyBinding(tagSearchMode);
    }
    
    private void initializeTagCategories() {
        // Create tag categories
        TAG_CATEGORIES.put("material", new TagCategory("Material", 
            Arrays.asList("wood", "stone", "metal", "gem", "organic", "magical")));
        TAG_CATEGORIES.put("purpose", new TagCategory("Purpose",
            Arrays.asList("tool", "weapon", "armor", "building", "decorative", 
                         "consumable", "magical", "transport")));
        TAG_CATEGORIES.put("rarity", new TagCategory("Rarity",
            Arrays.asList("common", "uncommon", "rare", "epic", "legendary", "unique")));
        TAG_CATEGORIES.put("element", new TagCategory("Element",
            Arrays.asList("fire", "water", "earth", "air", "light", "dark", 
                         "arcane", "nature")));
        TAG_CATEGORIES.put("origin", new TagCategory("Origin",
            Arrays.asList("overworld", "nether", "end", "dimension", "ancient", 
                         "modern", "futuristic")));
    }
    
    private void initializeTagPresets() {
        // Create useful tag presets
        TAG_PRESETS.add(new TagPreset("Mining", 
            Arrays.asList("material:stone", "purpose:tool", "rarity:common")));
        TAG_PRESETS.add(new TagPreset("Combat", 
            Arrays.asList("purpose:weapon", "purpose:armor", "rarity:uncommon")));
        TAG_PRESETS.add(new TagPreset("Magic", 
            Arrays.asList("material:magical", "purpose:magical", "element:arcane")));
        TAG_PRESETS.add(new TagPreset("Building", 
            Arrays.asList("purpose:building", "purpose:decorative", "material:stone")));
        TAG_PRESETS.add(new TagPreset("Rare Items", 
            Arrays.asList("rarity:rare", "rarity:epic", "rarity:legendary")));
    }
    
    private void registerNetwork() {
        // Register packet handlers for client-server communication
    }
    
    private void registerCapabilities() {
        // Register player tag capability
    }
    
    private void scanAllItemsForTags() {
        // Auto-tag all registered items based on properties
        for (ResourceLocation itemId : Item.REGISTRY.getKeys()) {
            Item item = Item.REGISTRY.getObject(itemId);
            if (item != null) {
                autoTagItem(item);
            }
        }
    }
    
    private void autoTagItem(Item item) {
        ItemStack stack = new ItemStack(item);
        String itemName = item.getRegistryName().toString().toLowerCase();
        
        // Analyze item and assign automatic tags
        List<String> autoTags = new ArrayList<>();
        
        // Material detection
        if (itemName.contains("wood") || itemName.contains("log") || itemName.contains("plank")) {
            autoTags.add("material:wood");
        } else if (itemName.contains("stone") || itemName.contains("cobble") || itemName.contains("brick")) {
            autoTags.add("material:stone");
        } else if (itemName.contains("iron") || itemName.contains("gold") || itemName.contains("diamond")) {
            autoTags.add("material:metal");
        } else if (itemName.contains("emerald") || itemName.contains("quartz") || itemName.contains("gem")) {
            autoTags.add("material:gem");
        }
        
        // Purpose detection
        if (item.getToolClasses(stack).contains("pickaxe") || 
            item.getToolClasses(stack).contains("axe") ||
            item.getToolClasses(stack).contains("shovel")) {
            autoTags.add("purpose:tool");
        } else if (itemName.contains("sword") || itemName.contains("bow") || itemName.contains("arrow")) {
            autoTags.add("purpose:weapon");
        } else if (itemName.contains("helmet") || itemName.contains("chestplate") || 
                   itemName.contains("leggings") || itemName.contains("boots")) {
            autoTags.add("purpose:armor");
        } else if (item.isFood()) {
            autoTags.add("purpose:consumable");
        }
        
        // Rarity estimation based on crafting cost
        if (itemName.contains("diamond") || itemName.contains("emerald") || itemName.contains("nether")) {
            autoTags.add("rarity:uncommon");
        } else if (itemName.contains("ender") || itemName.contains("dragon")) {
            autoTags.add("rarity:rare");
        } else {
            autoTags.add("rarity:common");
        }
        
        // Store auto-tags
        ItemTagManager.setAutoTags(item, autoTags);
    }
    
    // Tag Category class
    public static class TagCategory {
        public final String name;
        public final List<String> tags;
        public final Map<String, Integer> tagColors = new HashMap<>();
        
        public TagCategory(String name, List<String> tags) {
            this.name = name;
            this.tags = tags;
            
            // Assign colors to tags
            for (int i = 0; i < tags.size(); i++) {
                tagColors.put(tags.get(i), TAG_COLORS[i % TAG_COLORS.length]);
            }
        }
        
        public int getTagColor(String tag) {
            return tagColors.getOrDefault(tag, 0xFFFFFF);
        }
    }
    
    // Tag Preset class
    public static class TagPreset {
        public final String name;
        public final List<String> tags;
        
        public TagPreset(String name, List<String> tags) {
            this.name = name;
            this.tags = tags;
        }
        
        public void applyToItem(ItemStack stack) {
            ItemTagManager.addTags(stack, tags);
        }
    }
    
    // Core Tag Manager
    public static class ItemTagManager {
        private static final String TAG_KEY = "ThreeTags";
        
        public static boolean hasTags(ItemStack stack) {
            return stack.hasTagCompound() && stack.getTagCompound().hasKey(TAG_KEY);
        }
        
        public static List<String> getTags(ItemStack stack) {
            if (!hasTags(stack)) return Collections.emptyList();
            
            NBTTagList tagList = stack.getTagCompound().getTagList(TAG_KEY, 8);
            List<String> tags = new ArrayList<>();
            
            for (int i = 0; i < tagList.tagCount(); i++) {
                tags.add(tagList.getStringTagAt(i));
            }
            
            return tags;
        }
        
        public static void setTags(ItemStack stack, List<String> tags) {
            if (!stack.hasTagCompound()) {
                stack.setTagCompound(new NBTTagCompound());
            }
            
            NBTTagList tagList = new NBTTagList();
            for (String tag : tags) {
                tagList.appendTag(new net.minecraft.nbt.NBTTagString(tag));
            }
            
            stack.getTagCompound().setTag(TAG_KEY, tagList);
        }
        
        public static void addTag(ItemStack stack, String tag) {
            List<String> tags = getTags(stack);
            if (!tags.contains(tag)) {
                tags.add(tag);
                setTags(stack, tags);
            }
        }
        
        public static void addTags(ItemStack stack, List<String> newTags) {
            List<String> tags = getTags(stack);
            for (String tag : newTags) {
                if (!tags.contains(tag)) {
                    tags.add(tag);
                }
            }
            setTags(stack, tags);
        }
        
        public static void removeTag(ItemStack stack, String tag) {
            List<String> tags = getTags(stack);
            tags.remove(tag);
            setTags(stack, tags);
        }
        
        public static void clearTags(ItemStack stack) {
            if (stack.hasTagCompound()) {
                stack.getTagCompound().removeTag(TAG_KEY);
            }
        }
        
        public static boolean hasTag(ItemStack stack, String tag) {
            return getTags(stack).contains(tag);
        }
        
        public static boolean matchesAnyTag(ItemStack stack, List<String> searchTags) {
            List<String> itemTags = getTags(stack);
            for (String searchTag : searchTags) {
                if (itemTags.contains(searchTag)) {
                    return true;
                }
            }
            return false;
        }
        
        public static boolean matchesAllTags(ItemStack stack, List<String> searchTags) {
            List<String> itemTags = getTags(stack);
            return itemTags.containsAll(searchTags);
        }
        
        public static List<String> getAutoTags(Item item) {
            // Return pre-computed auto-tags
            return ItemAutoTagCache.getAutoTags(item);
        }
        
        public static void setAutoTags(Item item, List<String> tags) {
            ItemAutoTagCache.setAutoTags(item, tags);
        }
        
        public static String getTagDisplay(String fullTag) {
            String[] parts = fullTag.split(":");
            if (parts.length == 2) {
                String category = parts[0];
                String tag = parts[1];
                return TAG_CATEGORIES.containsKey(category) ? 
                    TAG_CATEGORIES.get(category).name + ": " + tag : fullTag;
            }
            return fullTag;
        }
        
        public static int getTagColor(String fullTag) {
            String[] parts = fullTag.split(":");
            if (parts.length == 2) {
                String category = parts[0];
                String tag = parts[1];
                if (TAG_CATEGORIES.containsKey(category)) {
                    return TAG_CATEGORIES.get(category).getTagColor(tag);
                }
            }
            return 0xFFFFFF;
        }
    }
    
    // Auto-tag cache
    public static class ItemAutoTagCache {
        private static final Map<Item, List<String>> CACHE = new HashMap<>();
        
        public static List<String> getAutoTags(Item item) {
            return CACHE.getOrDefault(item, Collections.emptyList());
        }
        
        public static void setAutoTags(Item item, List<String> tags) {
            CACHE.put(item, tags);
        }
        
        public static void clear() {
            CACHE.clear();
        }
    }
    
    // Player Tag Capability
    public static class PlayerTagData {
        private final Map<String, List<String>> favoriteTags = new HashMap<>();
        private final Map<String, Integer> tagUsageCount = new HashMap<>();
        private final List<String> recentSearches = new ArrayList<>();
        private String currentSearch = "";
        
        public void incrementTagUsage(String tag) {
            tagUsageCount.put(tag, tagUsageCount.getOrDefault(tag, 0) + 1);
        }
        
        public int getTagUsage(String tag) {
            return tagUsageCount.getOrDefault(tag, 0);
        }
        
        public void addFavoriteTag(String category, String tag) {
            favoriteTags.computeIfAbsent(category, k -> new ArrayList<>()).add(tag);
        }
        
        public void removeFavoriteTag(String category, String tag) {
            if (favoriteTags.containsKey(category)) {
                favoriteTags.get(category).remove(tag);
            }
        }
        
        public List<String> getFavoriteTags(String category) {
            return favoriteTags.getOrDefault(category, Collections.emptyList());
        }
        
        public void addSearch(String search) {
            recentSearches.remove(search);
            recentSearches.add(0, search);
            if (recentSearches.size() > 10) {
                recentSearches.remove(10);
            }
        }
        
        public List<String> getRecentSearches() {
            return new ArrayList<>(recentSearches);
        }
        
        public void setCurrentSearch(String search) {
            this.currentSearch = search;
            if (!search.isEmpty()) {
                addSearch(search);
            }
        }
        
        public String getCurrentSearch() {
            return currentSearch;
        }
        
        public NBTTagCompound serializeNBT() {
            NBTTagCompound compound = new NBTTagCompound();
            
            // Save favorite tags
            NBTTagCompound favorites = new NBTTagCompound();
            for (Map.Entry<String, List<String>> entry : favoriteTags.entrySet()) {
                NBTTagList list = new NBTTagList();
                for (String tag : entry.getValue()) {
                    list.appendTag(new net.minecraft.nbt.NBTTagString(tag));
                }
                favorites.setTag(entry.getKey(), list);
            }
            compound.setTag("favorites", favorites);
            
            // Save tag usage
            NBTTagCompound usage = new NBTTagCompound();
            for (Map.Entry<String, Integer> entry : tagUsageCount.entrySet()) {
                usage.setInteger(entry.getKey(), entry.getValue());
            }
            compound.setTag("usage", usage);
            
            // Save recent searches
            NBTTagList searches = new NBTTagList();
            for (String search : recentSearches) {
                searches.appendTag(new net.minecraft.nbt.NBTTagString(search));
            }
            compound.setTag("searches", searches);
            
            compound.setString("currentSearch", currentSearch);
            
            return compound;
        }
        
        public void deserializeNBT(NBTTagCompound compound) {
            favoriteTags.clear();
            tagUsageCount.clear();
            recentSearches.clear();
            
            // Load favorite tags
            if (compound.hasKey("favorites")) {
                NBTTagCompound favorites = compound.getCompoundTag("favorites");
                for (String category : favorites.getKeySet()) {
                    NBTTagList list = favorites.getTagList(category, 8);
                    List<String> tags = new ArrayList<>();
                    for (int i = 0; i < list.tagCount(); i++) {
                        tags.add(list.getStringTagAt(i));
                    }
                    favoriteTags.put(category, tags);
                }
            }
            
            // Load tag usage
            if (compound.hasKey("usage")) {
                NBTTagCompound usage = compound.getCompoundTag("usage");
                for (String tag : usage.getKeySet()) {
                    tagUsageCount.put(tag, usage.getInteger(tag));
                }
            }
            
            // Load recent searches
            if (compound.hasKey("searches")) {
                NBTTagList searches = compound.getTagList("searches", 8);
                for (int i = 0; i < searches.tagCount(); i++) {
                    recentSearches.add(searches.getStringTagAt(i));
                }
            }
            
            currentSearch = compound.getString("currentSearch");
        }
    }
}

// Tag GUI
@SideOnly(Side.CLIENT)
class GuiTagManager extends GuiScreen {
    private static final ResourceLocation BACKGROUND = 
        new ResourceLocation(THREETAGMod.MODID, "textures/gui/tag_manager.png");
    
    private final EntityPlayer player;
    private final InventoryPlayer inventory;
    private Slot hoveredSlot;
    private List<String> selectedTags = new ArrayList<>();
    private String searchText = "";
    private int guiLeft, guiTop, xSize = 256, ySize = 166;
    private boolean searchMode = false;
    private int scrollOffset = 0;
    private final int TAGS_PER_PAGE = 8;
    
    public GuiTagManager(EntityPlayer player) {
        this.player = player;
        this.inventory = player.inventory;
    }
    
    @Override
    public void initGui() {
        super.initGui();
        this.guiLeft = (this.width - this.xSize) / 2;
        this.guiTop = (this.height - this.ySize) / 2;
    }
    
    @Override
    public void drawScreen(int mouseX, int mouseY, float partialTicks) {
        this.drawDefaultBackground();
        
        // Draw background
        GlStateManager.color(1.0F, 1.0F, 1.0F, 1.0F);
        this.mc.getTextureManager().bindTexture(BACKGROUND);
        this.drawTexturedModalRect(guiLeft, guiTop, 0, 0, xSize, ySize);
        
        // Draw title
        String title = "THREETAG Manager";
        this.fontRenderer.drawString(title, 
            guiLeft + (xSize - fontRenderer.getStringWidth(title)) / 2,
            guiTop + 6, 0x404040);
        
        // Draw inventory
        drawInventorySlots();
        
        // Draw tag categories
        drawTagCategories(mouseX, mouseY);
        
        // Draw selected tags
        drawSelectedTags();
        
        // Draw search box
        drawSearchBox(mouseX, mouseY);
        
        // Draw tag suggestions
        if (!searchText.isEmpty()) {
            drawTagSuggestions(mouseX, mouseY);
        }
        
        super.drawScreen(mouseX, mouseY, partialTicks);
    }
    
    private void drawInventorySlots() {
        int startX = guiLeft + 8;
        int startY = guiTop + 20;
        
        RenderHelper.enableGUIStandardItemLighting();
        GlStateManager.pushMatrix();
        GlStateManager.translate(startX, startY, 0);
        GlStateManager.color(1.0F, 1.0F, 1.0F, 1.0F);
        GlStateManager.enableRescaleNormal();
        
        // Draw player inventory (hotbar + main)
        for (int row = 0; row < 4; row++) {
            for (int col = 0; col < 9; col++) {
                int slotIndex = col + row * 9;
                if (slotIndex < 36) {
                    ItemStack stack = inventory.mainInventory.get(slotIndex);
                    int x = col * 18;
                    int y = row * 18;
                    
                    // Draw slot background
                    this.drawTexturedModalRect(x, y, 176, 0, 18, 18);
                    
                    // Draw item
                    if (!stack.isEmpty()) {
                        this.itemRender.renderItemAndEffectIntoGUI(stack, x + 1, y + 1);
                        this.itemRender.renderItemOverlayIntoGUI(this.fontRenderer, stack, x + 1, y + 1, null);
                        
                        // Draw tags if item has them
                        List<String> tags = THREETAGMod.ItemTagManager.getTags(stack);
                        if (!tags.isEmpty()) {
                            drawItemTags(x + 1, y + 1, tags);
                        }
                    }
                    
                    // Check if mouse is over this slot
                    if (mouseX >= startX + x && mouseX < startX + x + 18 &&
                        mouseY >= startY + y && mouseY < startY + y + 18) {
                        hoveredSlot = new Slot(inventory, slotIndex, x, y) {
                            @Override
                            public boolean canTakeStack(EntityPlayer playerIn) {
                                return false;
                            }
                        };
                    }
                }
            }
        }
        
        GlStateManager.popMatrix();
        RenderHelper.disableStandardItemLighting();
    }
    
    private void drawItemTags(int x, int y, List<String> tags) {
        GlStateManager.pushMatrix();
        GlStateManager.translate(0, 0, 300); // Draw above items
        
        // Draw up to 3 tags
        for (int i = 0; i < Math.min(3, tags.size()); i++) {
            String tag = tags.get(i);
            int color = THREETAGMod.ItemTagManager.getTagColor(tag);
            int tagX = x + i * 5;
            int tagY = y - 4;
            
            // Draw colored tag indicator
            drawRect(tagX, tagY, tagX + 4, tagY + 4, color | 0xFF000000);
        }
        
        GlStateManager.popMatrix();
    }
    
    private void drawTagCategories(int mouseX, int mouseY) {
        int startX = guiLeft + 180;
        int startY = guiTop + 20;
        int categorySpacing = 25;
        
        int index = 0;
        for (Map.Entry<String, THREETAGMod.TagCategory> entry : 
             THREETAGMod.TAG_CATEGORIES.entrySet()) {
            
            int y = startY + index * categorySpacing;
            boolean mouseOver = mouseX >= startX && mouseX < startX + 70 &&
                               mouseY >= y && mouseY < y + 20;
            
            // Draw category background
            drawRect(startX, y, startX + 70, y + 20, 
                    mouseOver ? 0x80333333 : 0x80222222);
            
            // Draw category name
            fontRenderer.drawString(entry.getValue().name, 
                startX + 5, y + 6, 0xFFFFFF);
            
            // Draw arrow if expanded
            if (mouseOver) {
                fontRenderer.drawString("▶", startX + 60, y + 6, 0xFFFFFF);
                
                // Draw tags in this category
                drawCategoryTags(entry.getKey(), entry.getValue(), 
                               startX + 75, y, mouseX, mouseY);
            }
            
            index++;
        }
    }
    
    private void drawCategoryTags(String categoryId, THREETAGMod.TagCategory category, 
                                 int x, int y, int mouseX, int mouseY) {
        drawRect(x, y, x + 100, y + 150, 0x80222222);
        
        int tagY = y + 5;
        for (String tag : category.tags) {
            String fullTag = categoryId + ":" + tag;
            boolean selected = selectedTags.contains(fullTag);
            boolean mouseOver = mouseX >= x && mouseX < x + 95 &&
                               mouseY >= tagY && mouseY < tagY + 15;
            
            // Draw tag background
            drawRect(x + 2, tagY, x + 98, tagY + 15, 
                    selected ? 0x803366FF : 
                    mouseOver ? 0x80666666 : 0x80444444);
            
            // Draw tag color indicator
            int color = category.getTagColor(tag);
            drawRect(x + 3, tagY + 3, x + 8, tagY + 12, color | 0xFF000000);
            
            // Draw tag name
            fontRenderer.drawString(tag, x + 15, tagY + 4, 0xFFFFFF);
            
            tagY += 18;
        }
    }
    
    private void drawSelectedTags() {
        int startX = guiLeft + 8;
        int startY = guiTop + 100;
        
        fontRenderer.drawString("Selected Tags:", startX, startY, 0x404040);
        
        int tagX = startX;
        int tagY = startY + 12;
        
        for (String tag : selectedTags) {
            String display = THREETAGMod.ItemTagManager.getTagDisplay(tag);
            int color = THREETAGMod.ItemTagManager.getTagColor(tag);
            int width = fontRenderer.getStringWidth(display) + 10;
            
            if (tagX + width > guiLeft + 170) {
                tagX = startX;
                tagY += 20;
            }
            
            // Draw tag background
            drawRect(tagX, tagY, tagX + width, tagY + 15, 0x80222222);
            drawRect(tagX + 1, tagY + 1, tagX + 4, tagY + 14, color | 0xFF000000);
            
            // Draw tag text
            fontRenderer.drawString(display, tagX + 8, tagY + 4, 0xFFFFFF);
            
            // Draw remove button
            drawRect(tagX + width - 12, tagY + 3, tagX + width - 3, tagY + 12, 0x80FF3333);
            fontRenderer.drawString("×", tagX + width - 10, tagY + 4, 0xFFFFFF);
            
            tagX += width + 5;
        }
        
        // Apply tags button
        if (!selectedTags.isEmpty()) {
            drawRect(guiLeft + 8, tagY + 25, guiLeft + 80, tagY + 45, 0x8033CC33);
            fontRenderer.drawString("Apply Tags", guiLeft + 15, tagY + 32, 0xFFFFFF);
        }
    }
    
    private void drawSearchBox(int mouseX, int mouseY) {
        int boxX = guiLeft + 180;
        int boxY = guiTop + 140;
        
        drawRect(boxX, boxY, boxX + 70, boxY + 20, 0x80222222);
        fontRenderer.drawString("Search:", boxX + 5, boxY - 12, 0x404040);
        
        String displayText = searchMode ? searchText + "_" : "Click to search";
        fontRenderer.drawString(displayText, boxX + 5, boxY + 6, 
                               searchMode ? 0xFFFFFF : 0x888888);
    }
    
    private void drawTagSuggestions(int mouseX, int mouseY) {
        List<String> suggestions = getTagSuggestions(searchText);
        if (suggestions.isEmpty()) return;
        
        int startX = guiLeft + 180;
        int startY = guiTop + 165;
        int maxHeight = 100;
        
        drawRect(startX, startY, startX + 100, 
                startY + Math.min(suggestions.size() * 15, maxHeight), 0x80222222);
        
        for (int i = 0; i < Math.min(suggestions.size(), maxHeight / 15); i++) {
            String suggestion = suggestions.get(i);
            int y = startY + i * 15;
            boolean mouseOver = mouseX >= startX && mouseX < startX + 100 &&
                               mouseY >= y && mouseY < y + 15;
            
            if (mouseOver) {
                drawRect(startX, y, startX + 100, y + 15, 0x80666666);
            }
            
            fontRenderer.drawString(suggestion, startX + 5, y + 3, 0xFFFFFF);
        }
    }
    
    private List<String> getTagSuggestions(String query) {
        List<String> suggestions = new ArrayList<>();
        query = query.toLowerCase();
        
        for (Map.Entry<String, THREETAGMod.TagCategory> entry : 
             THREETAGMod.TAG_CATEGORIES.entrySet()) {
            for (String tag : entry.getValue().tags) {
                String fullTag = entry.getKey() + ":" + tag;
                if (fullTag.toLowerCase().contains(query)) {
                    suggestions.add(fullTag);
                }
            }
        }
        
        return suggestions.stream().limit(10).collect(Collectors.toList());
    }
    
    @Override
    protected void mouseClicked(int mouseX, int mouseY, int mouseButton) {
        super.mouseClicked(mouseX, mouseY, mouseButton);
        
        // Check search box click
        int boxX = guiLeft + 180;
        int boxY = guiTop + 140;
        if (mouseX >= boxX && mouseX < boxX + 70 &&
            mouseY >= boxY && mouseY < boxY + 20) {
            searchMode = !searchMode;
            return;
        }
        
        // Check tag category clicks
        int startX = guiLeft + 180;
        int startY = guiTop + 20;
        
        int index = 0;
        for (Map.Entry<String, THREETAGMod.TagCategory> entry : 
             THREETAGMod.TAG_CATEGORIES.entrySet()) {
            
            int y = startY + index * 25;
            if (mouseX >= startX && mouseX < startX + 70 &&
                mouseY >= y && mouseY < y + 20) {
                
                // Category clicked - check if tags are shown
                // For now, just toggle selection of first tag
                String firstTag = entry.getKey() + ":" + entry.getValue().tags.get(0);
                toggleTagSelection(firstTag);
                return;
            }
            
            // Check tags in expanded category
            if (mouseX >= startX + 75 && mouseX < startX + 175 &&
                mouseY >= y && mouseY < y + 150) {
                
                int tagIndex = (mouseY - y - 5) / 18;
                if (tagIndex >= 0 && tagIndex < entry.getValue().tags.size()) {
                    String tag = entry.getValue().tags.get(tagIndex);
                    String fullTag = entry.getKey() + ":" + tag;
                    toggleTagSelection(fullTag);
                    return;
                }
            }
            
            index++;
        }
        
        // Check apply tags button
        if (!selectedTags.isEmpty()) {
            int buttonY = findApplyButtonY();
            if (mouseX >= guiLeft + 8 && mouseX < guiLeft + 80 &&
                mouseY >= buttonY && mouseY < buttonY + 20) {
                applyTagsToHoveredItem();
                return;
            }
        }
        
        // Check selected tags for removal
        checkSelectedTagRemoval(mouseX, mouseY);
    }
    
    @Override
    protected void keyTyped(char typedChar, int keyCode) {
        if (searchMode) {
            if (keyCode == Keyboard.KEY_ESCAPE) {
                searchMode = false;
            } else if (keyCode == Keyboard.KEY_RETURN) {
                if (!searchText.isEmpty()) {
                    addTagFromSearch(searchText);
                    searchText = "";
                }
                searchMode = false;
            } else if (keyCode == Keyboard.KEY_BACK) {
                if (!searchText.isEmpty()) {
                    searchText = searchText.substring(0, searchText.length() - 1);
                }
            } else if (Character.isLetterOrDigit(typedChar) || typedChar == ':' || typedChar == ' ') {
                searchText += typedChar;
            }
        } else {
            super.keyTyped(typedChar, keyCode);
        }
    }
    
    private void toggleTagSelection(String tag) {
        if (selectedTags.contains(tag)) {
            selectedTags.remove(tag);
        } else {
            if (selectedTags.size() < 3) {
                selectedTags.add(tag);
            } else {
                // Can only select 3 tags at once
                player.sendMessage(new TextComponentString(
                    TextFormatting.RED + "You can only select 3 tags at once!"));
            }
        }
    }
    
    private void addTagFromSearch(String search) {
        // Check if search matches a valid tag
        for (Map.Entry<String, THREETAGMod.TagCategory> entry : 
             THREETAGMod.TAG_CATEGORIES.entrySet()) {
            for (String tag : entry.getValue().tags) {
                String fullTag = entry.getKey() + ":" + tag;
                if (fullTag.equalsIgnoreCase(search)) {
                    toggleTagSelection(fullTag);
                    return;
                }
            }
        }
        
        player.sendMessage(new TextComponentString(
            TextFormatting.RED + "Invalid tag: " + search));
    }
    
    private int findApplyButtonY() {
        // Calculate where the apply button is based on selected tags
        int startY = guiTop + 100;
        int tagY = startY + 12;
        int tagX = guiLeft + 8;
        
        for (String tag : selectedTags) {
            String display = THREETAGMod.ItemTagManager.getTagDisplay(tag);
            int width = fontRenderer.getStringWidth(display) + 10;
            
            if (tagX + width > guiLeft + 170) {
                tagX = guiLeft + 8;
                tagY += 20;
            }
            
            tagX += width + 5;
        }
        
        return tagY + 25;
    }
    
    private void applyTagsToHoveredItem() {
        if (hoveredSlot != null && hoveredSlot.getHasStack()) {
            ItemStack stack = hoveredSlot.getStack();
            THREETAGMod.ItemTagManager.setTags(stack, selectedTags);
            
            player.sendMessage(new TextComponentString(
                TextFormatting.GREEN + "Applied " + selectedTags.size() + " tags to " + 
                stack.getDisplayName()));
            
            selectedTags.clear();
        } else {
            player.sendMessage(new TextComponentString(
                TextFormatting.RED + "No item selected! Hover over an item in your inventory."));
        }
    }
    
    private void checkSelectedTagRemoval(int mouseX, int mouseY) {
        int startX = guiLeft + 8;
        int startY = guiTop + 112;
        int tagX = startX;
        int tagY = startY;
        
        for (String tag : selectedTags) {
            String display = THREETAGMod.ItemTagManager.getTagDisplay(tag);
            int width = fontRenderer.getStringWidth(display) + 10;
            
            if (tagX + width > guiLeft + 170) {
                tagX = startX;
                tagY += 20;
            }
            
            // Check remove button
            if (mouseX >= tagX + width - 12 && mouseX < tagX + width - 3 &&
                mouseY >= tagY + 3 && mouseY < tagY + 12) {
                selectedTags.remove(tag);
                return;
            }
            
            tagX += width + 5;
        }
    }
    
    @Override
    public boolean doesGuiPauseGame() {
        return false;
    }
}

// Event Handler
class TagEventHandler {
    @SubscribeEvent
    @SideOnly(Side.CLIENT)
    public void onKeyInput(InputEvent.KeyInputEvent event) {
        Minecraft mc = Minecraft.getMinecraft();
        
        if (THREETAGMod.openTagGui.isPressed() && mc.currentScreen == null) {
            mc.displayGuiScreen(new GuiTagManager(mc.player));
        }
        
        // Quick tag keys
        if (mc.currentScreen instanceof GuiContainer) {
            Slot hoveredSlot = ((GuiContainer) mc.currentScreen).getSlotUnderMouse();
            if (hoveredSlot != null && hoveredSlot.getHasStack()) {
                ItemStack stack = hoveredSlot.getStack();
                
                if (THREETAGMod.quickTag1.isPressed()) {
                    THREETAGMod.ItemTagManager.addTag(stack, "material:metal");
                } else if (THREETAGMod.quickTag2.isPressed()) {
                    THREETAGMod.ItemTagManager.addTag(stack, "purpose:tool");
                } else if (THREETAGMod.quickTag3.isPressed()) {
                    THREETAGMod.ItemTagManager.addTag(stack, "rarity:uncommon");
                }
            }
        }
    }
    
    @SubscribeEvent
    public void onItemTooltip(net.minecraftforge.event.entity.player.ItemTooltipEvent event) {
        ItemStack stack = event.getItemStack();
        List<String> tags = THREETAGMod.ItemTagManager.getTags(stack);
        
        if (!tags.isEmpty()) {
            event.getToolTip().add(TextFormatting.DARK_GRAY + "Tags:");
            for (String tag : tags) {
                int color = THREETAGMod.ItemTagManager.getTagColor(tag);
                String display = THREETAGMod.ItemTagManager.getTagDisplay(tag);
                event.getToolTip().add(TextFormatting.GRAY + "  " + 
                    net.minecraft.util.text.TextFormatting.fromColorIndex(color) + "● " + 
                    TextFormatting.WHITE + display);
            }
        }
        
        // Show auto-tags in shift mode
        if (GuiScreen.isShiftKeyDown()) {
            List<String> autoTags = THREETAGMod.ItemTagManager.getAutoTags(stack.getItem());
            if (!autoTags.isEmpty()) {
                event.getToolTip().add(TextFormatting.DARK_GRAY + "Auto-tags:");
                for (String tag : autoTags) {
                    event.getToolTip().add(TextFormatting.DARK_GRAY + "  " + tag);
                }
            }
        }
    }
}

