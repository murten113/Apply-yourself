# Layer Troubleshooting Guide

## Updated Code
The code now checks **ALL layers** regardless of settings, but you should still verify your setup.

## How to Check Layers in Unity

### 1. Check Garden Plot Layers
1. Select a **GardenPlot** GameObject in Hierarchy
2. Look at the top of Inspector (next to Tag dropdown)
3. **Layer** should show the layer name
4. Default layer is usually "Default" (layer 0)

### 2. Check Physics2D Layer Collision Matrix
1. Go to **Edit → Project Settings → Physics 2D**
2. Scroll down to **Layer Collision Matrix**
3. Make sure your plot's layer can interact with other layers
4. Check the boxes where layers intersect (they should be checked by default)

### 3. Verify Collider Settings
For each GardenPlot:
1. Select the plot
2. Check **Collider2D** component:
   - **Enabled**: ✓ (must be checked)
   - **Is Trigger**: ✗ (should be UNCHECKED for click detection)
   - **Size**: Should cover the plot area

### 4. Check Camera Settings
1. Select **Main Camera**
2. **Culling Mask**: Should include the layer your plots are on
   - Usually "Everything" or at least "Default"
3. **Projection**: Orthographic
4. **Position Z**: Should be negative (e.g., -10) looking at Z=0

## Quick Fixes

### If Colliders Still Don't Work:

1. **Reset Layer to Default**:
   - Select all GardenPlots
   - Set Layer to "Default" (layer 0)
   - This is the safest option

2. **Check Collider is Enabled**:
   - Select each plot
   - In Collider2D component, ensure checkbox is checked

3. **Verify Collider Size**:
   - Select plot
   - In Scene view, you should see green wireframe around the plot
   - If you don't see it, the collider might be too small
   - Adjust **Size** in BoxCollider2D to make it larger

4. **Check Collider Position**:
   - Collider should be at the same position as the GameObject
   - If using BoxCollider2D, **Offset** should be (0, 0, 0)

## Debug Information

When you run the game, check the Console for:
- `"Found X collider(s) at position"` - Shows colliders were detected
- `"Collider X: [name] on layer Y"` - Shows which layer each collider is on
- `"No collider at mouse position"` - Means no collider found (check layers/settings)

## Common Issues

### Issue: "No collider at mouse position"
**Solutions**:
1. Check collider is enabled
2. Check collider is NOT a trigger
3. Check layer collision matrix
4. Verify camera can see the layer
5. Check collider size covers the plot

### Issue: Colliders found but no GardenPlot component
**Solution**: Make sure GardenPlot script is attached to the GameObject

### Issue: Wrong layer detected
**Solution**: The code now checks all layers, but you can set plots to "Default" layer for consistency

## Testing Steps

1. **Run the game**
2. **Click on a plot** (with Shovel selected)
3. **Check Console** - You should see:
   - "Found X collider(s) at position"
   - "Clicked on plot: [name]"
   - "Using shovel on plot"
   - "Dead plant removed!"

4. **If you see "No collider at mouse position"**:
   - Check the debug info above
   - Verify collider settings
   - Check layer settings

## Fallback System

Even if colliders don't work, the code has a **fallback system**:
- For drag & drop: Finds closest GardenPlot from GameManager's list
- Make sure GardenPlots are in GameManager's `gardenPlots` list
- The fallback works within `maxDropDistance` (default 2 units)

## Still Not Working?

1. **Check Console messages** - They tell you exactly what's wrong
2. **Verify all GardenPlots are in GameManager's list**
3. **Try the fallback** - Drag seed packet near a plot (within 2 units)
4. **Check GameObject positions** - Make sure plots are at Z=0 and camera can see them
