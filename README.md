Logan's Slewed Beer Factory Defacement Adventure
================================================

This is my game that I made for [The Arbitrary Game Jam #18](http://jams.gamejolt.io/tagjam18), it was created in about 72 hours. (I've been wanting to do a game jam for a long time, but things never worked out. So this is actually my first game jam game ever.)

The themes for this jam were:
* Slewed (Slang for "drunk")
* Defacement
* King Charles Spaniel

Additionally, the host, [@madmarcel](https://twitter.com/madmarcel), chose the following two bonus themes:
* Adolescence
* Gigantic

I opted to use all of the themes because the idea that started this game included most of them.

# Controls 
* Esc to exit
* WASD or Arrows to move
* Shift to sprint
* Space to start tagging (spray graffiti)

## When tagging:
* Move the mouse to aim
* Left-click to spray paint
* Right-click to cycle colors
* Enter to finish tagging

## "Cheats":
* N to disable the drunk screen effects (Nice for admiring your artwork, even if the effect is neat.)
* M simulate drinking a beer

# Story
You play as Logan, a young *adolesent* who likes to get *slewed* by drinking *gigantic* beers and *deface* the insides of buildings with graffiti of his favorite type of dog, the *King Charles Spaniel*.

Of course, nothing so edgy and rebelious is ever that easy. You must also help Logan avoid guards inside the Gigantic TAG Beer factory, the latest canvas for his artwork.

Note that Logan can only paint while slewed. He also has trouble seeing the ideal locations for his artwork when he is sober, so you might not see them until he starts to get tipsy.

# Development
I developed this game alone "from scratch" using C# and the SharpDX Toolkit. This was my first time really using the Sharp DX Toolkit, but I have a decent amount of experience with SharpDX in general.
All of the code was written specifically for this game, and all of the assets were created/found during the jam. You can find the third party assets at the end of this document.

## Other tools used:
* Git
* Visual Studio 2013
* Photoshop CS5.5
* Blender

# Missing features / Known issues
Being a time-constrained game jam, I didn't quite finish everything I set out to do. In particular:
* Right now Logan is a boring gray cylinder, he needs a model of some sort.
* I never got around to adding guards :(
* There's only the one level
* My vision included lighting, so you'd have to explore more to find everything and there would be a stealth element to it. Right now the entire level is lit.
* There's no sound :(

# Asset Credits
* Concrete textures by p0ss - http://opengameart.org/content/p0sss-texture-pack-1
* Brick texture by Georges "TRaK" Grondin and Yves "Evillair" Allaire - http://opengameart.org/content/filth-texture-set-trakbricks2gjpg
* "Sozzled Brain Scoop" by madmarcel was used in beer bottle texture - http://opengameart.org/content/sozzled-brain-scoop
* "Sniglet" font used by The Leauge of Movable Type - https://www.theleagueofmoveabletype.com/sniglet
* Drunk full screen shader was based on the radial blur shader by nVidia - http://developer.download.nvidia.com/shaderlibrary/webpages/hlsl_shaders.html#post_radialBlur
* "League Gothic" font by The League of Movable Type - https://www.theleagueofmoveabletype.com/league-gothic
* Picture of a cavalier king charles spaniel by High-town-wp - Used for the graffiti template - https://commons.wikimedia.org/wiki/File:Cavalier-king-charles-spaniels.jpg

# License Information
Unless otherwise noted, C# code, HLSL code, and XML documents are licensed under the MIT License with the no endorsement clause, see License.txt for details.

The following are licensed under the Creative Commons Attribution-ShareAlike 3.0 Unported License http://creativecommons.org/licenses/by-sa/3.0/
* BeerBottleTexture.dds
* BeerBottleTexture.psd

The following are licensed under the Creative Commons Attribution-ShareAlike 4.0 International License http://creativecommons.org/licenses/by-sa/4.0/
* BeerBottle.blend
* BeerBottle.fbx
* Template1.png
* Template1.psd
* Tileset.png
* Tileset.psd
* PaintSpray.png
* Level1.tmx

For assets taken from opengameart, see the licenses on their respective pages.

For Structures.fxh, see the notice included at the top of the file.
