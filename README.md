# SkinGore URP

SkinGore is a dynamic gore and injury system by [Zulubo](www.zulubo.com) for Vertigo 2. It can be used with skinned meshes and is very performant even with many instances in a scene.

SkinGore URP is a port to URP, with a series of refinements for low GPU systems, by Super Whimsy.

Updates:
* Converted SkinGore to ShaderGraph (URP) and revised SkinGore methods. 
* Optimized to use a single display material and single mesh renderer, versus an overlaid mesh.
* Added in optimizations to reduce performance spikes.
* Added in 'Detail' material parameter. Low detail means broad, undetailed damage. High detail means smaller, more intricate damage. Pairs well with 'Hardness'.
* Added randomized seed for 'DetailMap' offset for more unique gore randomness.
* Added in tiling and offset fields for all textures.
* Added option to increase normal strength above 1 for mobile.


## Demo

SkinGore > Demo > SkinGoreDemo

Click on the mannequins to damage them. >:D

Note: by default, it will take 1-2 seconds to initialize SkinGore URP due to an optional optimization feature called 'Delay between stages'. More information on this optimization below.


## How It Works - by Zubulo
Every time damage is taken, the character mesh is rendered in its UV coordinates with a special shader that draws a white blob at the damage location. This hit buffer is combined additively with a persistent damage accumulation buffer that holds all the previous hits as well. Finally, this is used as a mask for a material that reveals blood and gore on the character. Since everything here happens on the GPU, it's insanely efficient, especially with the modest texture sizes that are required (64x64 by default). 


## Basics - by Zubulo
Minimal asset preparation is needed. The system uses the second UV channel if available, as some characters may have overlapping UVs in the first one. If needed, create a second UV map with no overlap. If no second UV channel is present, it will default back to the first one.

Add a SkinGoreRenderer component to your character, and select a target mesh and decal material. Example gore materials are included that you are welcome to use. The SkinGoreRenderer will automatically create a duplicate of the skinned mesh with the decal material applied when needed.

To add damage to your character, call `SkinGoreRenderer.AddDamage()`. Give it a world space position, radius, and strength for the damage. All calculations are done on the GPU and are super fast, so don't worry about calling this a lot.


## Performance
The Skin Gore Renderer has two new performance focused inspector fields: 'Cooldown Frames' and 'Delay Between Stages'.

'Cooldown Frames' is an optional feature to block additional damage updates for a set number of frames. For example, if you're aiming for 72 FPS and set cooldown frames to 31 FPS, then a player will have a half second cooldown before they are able to add visual damage to a character. If this isn't needed it can be set to 0.

'Delay Between Stages' is an optional time delay that allow you to space out the creation of the SkinGore textures and materials. This creation happens at start and is intensive, leading to reduced performance. A slight delay distributes this work and improves performance. If this isn't needed it can be set to 0.


## Initial SkinGore Project by Zubulo
https://github.com/zulubo/SkinGore

## Changelog
### v1 / 6/7/24 - Initial release!