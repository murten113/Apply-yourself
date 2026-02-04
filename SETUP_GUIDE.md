# Unity 2D Garden Game - Complete Setup Guide

## Canvas Setup for Unity 2D

### 1. Create Canvas (if not exists)
1. Right-click in Hierarchy → **UI → Canvas**
2. This creates:
   - Canvas (main UI container)
   - EventSystem (handles UI input)
   - GraphicRaycaster (automatically added to Canvas)

### 2. Canvas Settings
- Select **Canvas** in Hierarchy
- In Inspector, Canvas component:
  - **Render Mode**: Screen Space - Overlay (default)
  - **Pixel Perfect**: ✓ (optional, for crisp UI)
  - **Sort Order**: 0 (default)

### 3. Canvas Scaler (Important!)
- Select **Canvas**
- Add **Canvas Scaler** component if not present
- Settings:
  - **UI Scale Mode**: Scale With Screen Size
  - **Reference Resolution**: 1920 x 1080 (or your target resolution)
  - **Screen Match Mode**: Match Width Or Height
  - **Match**: 0.5 (balances width/height)

---

## UI Elements Setup

### Tool Buttons (Shovel, Watering Can)

1. **Create Shovel Button**:
   - Right-click Canvas → **UI → Button - TextMeshPro** (or regular Button)
   - Rename to "ShovelButton"
   - Position it (e.g., top-left)
   - Add TextMeshPro text child (or Text) → Change text to "Shovel"

2. **Create Watering Can Button**:
   - Right-click Canvas → **UI → Button - TextMeshPro**
   - Rename to "WateringCanButton"
   - Position next to Shovel button
   - Add TextMeshPro text child → Change text to "Watering Can"

3. **Connect Buttons to GameManager**:
   - Select **ShovelButton**
   - In Inspector, find **Button** component
   - Scroll to **OnClick()** section
   - Click **+** to add event
   - Drag **GameManager** GameObject from Hierarchy into the object field
   - In dropdown: **GameManager → SelectShovel()**
   
   - Repeat for **WateringCanButton**:
     - Drag GameManager → Select **GameManager → SelectWateringCan()**

4. **Assign Buttons in GameManager**:
   - Select **GameManager** in Hierarchy
   - In Inspector, find **Tool Selection Visual Feedback**
   - Drag **ShovelButton** into `shovelButton` field
   - Drag **WateringCanButton** into `wateringCanButton` field

---

### Seed Packet UI Elements (Drag & Drop)

1. **Create Seed Packet Container**:
   - Right-click Canvas → **UI → Panel** (or Empty GameObject)
   - Rename to "SeedPacketsContainer"
   - Position it (e.g., bottom of screen)

2. **Create Seed Packet 1 (Yellow)**:
   - Right-click SeedPacketsContainer → **UI → Image**
   - Rename to "SeedPacket_Yellow"
   - Add **SeedPacket** script component
   - In SeedPacket component:
     - Assign **Plant Data** (your YellowFlowerData ScriptableObject)
   - Add **Image** component if not present
   - Set size (e.g., 100x100 pixels)
   - Color will auto-set based on PlantData

3. **Create Seed Packet 2 (Red)**:
   - Duplicate SeedPacket_Yellow
   - Rename to "SeedPacket_Red"
   - Assign RedFlowerData to Plant Data

4. **Create Seed Packet 3 (Purple)**:
   - Duplicate SeedPacket_Yellow
   - Rename to "SeedPacket_Purple"
   - Assign PurpleFlowerData to Plant Data

5. **Make Seed Packets Draggable**:
   - Each SeedPacket GameObject needs:
     - **Image** component (for visual)
     - **SeedPacket** script (for drag functionality)
     - **CanvasGroup** (added automatically by script)

---

### UI Text Elements (Timer, Score)

1. **Create Timer Text**:
   - Right-click Canvas → **UI → Text - TextMeshPro**
   - Rename to "TimerText"
   - Position (e.g., top-right)
   - Set text to "05:00"
   - Style as needed

2. **Create Score Text**:
   - Right-click Canvas → **UI → Text - TextMeshPro**
   - Rename to "ScoreText"
   - Position next to Timer
   - Set text to "Score: 0"

3. **Assign to GameManager**:
   - Select **GameManager**
   - In **UI** section:
     - Drag **TimerText** into `timerText` field
     - Drag **ScoreText** into `scoreText` field

---

### Game Over Panel

1. **Create Game Over Panel**:
   - Right-click Canvas → **UI → Panel**
   - Rename to "GameOverPanel"
   - Set size to fill screen
   - Set color (semi-transparent black)
   - **Disable** it (uncheck checkbox in Inspector)

2. **Add Final Score Text**:
   - Right-click GameOverPanel → **UI → Text - TextMeshPro**
   - Rename to "FinalScoreText"
   - Center it on panel
   - Set text to "Final Score: 0"

3. **Assign to GameManager**:
   - Select **GameManager**
   - Drag **GameOverPanel** into `gameOverPanel` field
   - Drag **FinalScoreText** into `finalScoreText` field

---

## Scene Setup (Garden Plots)

### Garden Plot Setup

1. **Create Garden Plot GameObject**:
   - Right-click in Hierarchy → **2D Object → Sprite → Square** (or Empty GameObject)
   - Rename to "GardenPlot_1"
   - Add **GardenPlot** script component
   - Add **SpriteRenderer** component (if not present)
   - Add **BoxCollider2D** component:
     - Set size to cover the plot area (e.g., 1x1)
     - **Is Trigger**: Unchecked

2. **Configure GardenPlot Component**:
   - **Plot Renderer**: Drag the SpriteRenderer component
   - **Plant Prefab**: Drag your Plant prefab
   - **Is Unlocked**: ✓ (for starting plots)
   - **Has Dead Plant**: ✓ (for starting state)

3. **Repeat for all plots** (GardenPlot_2, GardenPlot_3, etc.)

4. **Add to GameManager**:
   - Select **GameManager**
   - In **Garden Plots** list:
     - Set **Size** to number of plots
     - Drag each GardenPlot into the list slots
   - OR: Script will auto-find them if list is empty

---

## Camera Setup

1. **Select Main Camera**:
   - In Hierarchy, select **Main Camera**

2. **Camera Settings**:
   - **Projection**: Orthographic
   - **Size**: 5 (adjust to fit your scene)
   - **Position**: X=0, Y=0, Z=-10 (looking at Z=0)
   - **Clear Flags**: Solid Color
   - **Background**: Your background color

---

## PlantData ScriptableObjects

1. **Create PlantData Assets**:
   - Right-click in Project window → **Create → Garden → Plant Data**
   - Create 3 assets:
     - "YellowFlowerData"
     - "RedFlowerData"
     - "PurpleFlowerData"

2. **Configure Each PlantData**:
   - **Plant Name**: "Yellow Flower" / "Red Flower" / "Purple Flower"
   - **Plant Type**: YellowFlower / RedFlower / PurpleFlower
   - **Plant Color**: Yellow / Red / Purple
   - **Growth Time**: 10 / 6 / 15
   - **Point Value**: 10 / 20 / 5
   - **Maintenance Bonus**: 5 / 10 / 3

3. **Assign to GameManager**:
   - Select **GameManager**
   - In **Available Plant Types**:
     - Set **Size** to 3
     - Drag all 3 PlantData assets into the list

4. **Assign to Seed Packets**:
   - Select each SeedPacket GameObject
   - In **SeedPacket** component:
     - Drag corresponding PlantData into **Plant Data** field

---

## Plant Prefab Setup

1. **Create Plant Prefab**:
   - Right-click in Hierarchy → **Create Empty**
   - Rename to "PlantPrefab"
   - Add **SpriteRenderer** component
   - Add **Plant** script component
   - Leave **Plant Data** empty (set at runtime)

2. **Save as Prefab**:
   - Drag from Hierarchy to Project window (Prefabs folder)
   - Delete from scene (keep prefab)

3. **Assign to GardenPlots**:
   - Select each GardenPlot
   - Drag PlantPrefab into **Plant Prefab** field

---

## EventSystem Check

1. **Verify EventSystem exists**:
   - Should be created automatically with Canvas
   - If missing: Right-click Hierarchy → **UI → EventSystem**

2. **EventSystem Settings**:
   - **First Selected**: None (or your first button)
   - **Send Navigation Events**: ✓

---

## Testing Checklist

### Button Functionality:
- [ ] Shovel button changes tool (check Console for "Selected Shovel")
- [ ] Watering Can button changes tool (check Console for "Selected Watering Can")
- [ ] Button colors change (green when selected)

### Clicking Plots:
- [ ] Click plot with Shovel selected → Dead plant removed (check Console)
- [ ] Click plot with Watering Can → Plant watered (if plant exists)

### Drag & Drop:
- [ ] Drag seed packet → It follows mouse
- [ ] Drop on plot → Plant appears (check Console for success message)
- [ ] Seed packet returns to original position

### Debug Console:
- Open **Window → General → Console**
- Check for error messages
- Look for debug logs showing what's happening

---

## Common Issues & Solutions

### Buttons Don't Work:
1. Check EventSystem exists in scene
2. Verify buttons are connected to GameManager methods
3. Check GameManager has button references assigned
4. Look in Console for errors

### Can't Click Plots:
1. Ensure plots have Collider2D components
2. Check collider size covers the plot
3. Verify camera can see plots (check Scene view)
4. Check Console for "No collider at mouse position" messages

### Drag & Drop Doesn't Work:
1. Ensure SeedPacket has PlantData assigned
2. Check GardenPlots are in GameManager's list
3. Verify plots have Collider2D
4. Check Console for drop position and detection messages

### UI Not Visible:
1. Check Canvas Render Mode (should be Screen Space - Overlay)
2. Verify UI elements are children of Canvas
3. Check UI element positions (might be off-screen)
4. Ensure Canvas Scaler is set up correctly

---

## Quick Reference: Hierarchy Structure

```
Scene
├── Main Camera
├── EventSystem
├── Canvas
│   ├── ShovelButton
│   ├── WateringCanButton
│   ├── TimerText
│   ├── ScoreText
│   ├── SeedPacketsContainer
│   │   ├── SeedPacket_Yellow
│   │   ├── SeedPacket_Red
│   │   └── SeedPacket_Purple
│   └── GameOverPanel
│       └── FinalScoreText
├── GameManager
├── GardenPlot_1
├── GardenPlot_2
├── GardenPlot_3
└── ... (more plots)
```

---

## Final Steps

1. **Run the game** (Play button)
2. **Check Console** for any errors or debug messages
3. **Test each feature**:
   - Click shovel button → Click plot → Dead plant removed
   - Drag seed packet → Drop on plot → Plant appears
   - Click watering can button → Click plot with plant → Plant watered

If something doesn't work, check the Console messages - they will tell you exactly what's wrong!
