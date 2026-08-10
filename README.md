# Embodied

https://github.com/EatenBaguette/EP491-Project

## Description

This project is a multimodal interactive installation that aims to
connect the kinetic with the visual and auditory modes using the gestures and positions of humans to drive a procedurally generated audiovisual system. The experience will involve the exploration of potential gesture combinations, such as in online particle generators where combining particles creates new types of particles. It will also involve the exploration of a virtual space as the participants progress through a short story.


## Tools and tech

Machine Learning

1. Pose landmark recognition using the YOLO pose model, trained on the COCO dataset.
2. Dynamic Time Warping (DTW) for gesture recognition. Based on the RTML Unity package. It was heavily modified as it did not implement true DTW. 
3. ChatGPT and Junie for help with code.

Other Tools

- realtime audio generation in Unity
	- Faust or Csound
	- custom Wwise signal generation plugins
- controlling the color and intensity of LED lights from Unity
- ShaderGraph for visuals

## Challenges and Areas of Support
1. Staying on track when it comes to composing and sound designing
2. Realtime visual generation using ShaderGraph and other Unity effects
3. People to test
4. Help with story writing

## Outcomes

### Good: Must do
- a Unity audiovisual system that responds to gestures
- 2-3 main states with transitions
- meaning 3-4 main music states with realtime variations/layering
- works for a single participant

### Better: Should do
- a character
- a progressable storyline
- various musical motifs are assigned to certain combinations of gestures to create a large number of combinations of music generation in addition to larger overall states
- for example, perhaps a rhythmic motif could map to a gesture combination and instrumentation to another, so when combined, it creates a unique section with the rhythm and instrumentation
- works for two partipants

### Best: Could do
- multiple endings
- extremely smooth transitions, feels easy to use
- works for "unlimited" participants who come and go


# The Process
## Exploration of Blender
https://leegriggs.com/bifrost-color-strands
https://www.youtube.com/watch?v=0XAXqMhvtVU
https://www.youtube.com/watch?v=VYyXDyRZwtw
https://www.youtube.com/watch?v=L6ACHU7zIWI
https://www.youtube.com/watch?v=eqLA7oJkLyM
https://www.youtube.com/watch?v=Lj2EBG2_ooQ
https://www.youtube.com/watch?v=wvK6MNlmCCE

## Import Model to Unity

Video demonstration: https://drive.google.com/file/d/17FweLmu4y49teMO--eSW9yjIq7KeXVXD/view?usp=drive_link 

Large glb file needed to run project: https://www.icloud.com/iclouddrive/0b4v8cb1RspI2TMfxygfHUD8Q#Fractal_Noise

The main issue was that to "grow" the model in realtime, the entire mesh has to be allocated in memory, meaning I have to find a different way to animate the geometry repetitions. Instead, the goal is to store a float percentage of total repetitions (repetition progress) in the R value of a color vector on each vertex. In unity Shader Graph, this value can be used to show or hide the geometry.

Update: I downloaded the package from this website (https://github.com/atteneder/glTFast) for converting glb files to prefabs. However, the vertex color data didn't work. I tested it by making a shader graph with the vertex color connected to base color. It should have been a gradient from black to red but it was all gray.

Update: It turns out the problem was during export from Blender. I was correct in using vertex colors since they can easily be read by ShaderGraph. The issue was that I had to select the attribute to export into the vertex colors in the export window. There was a dropdown menu with different ways to set the vertex colors, and I had to select By Name and type the name of the attribute I stored in the geometry nodes (which I called v_color).

I used a small number of points (6) to determine that the iteration process was working properly (tested by setting the base color to the vertex color, which should gradient from black to red as the iterations progress). It worked! I then recreated the final geometry and exported it. I created a shader that sets the alpha value based on the iteration progress and I was able to slide the default value and see the geometry grow!

Next, I created a basic slider UI with target value and speed. The target value sets the target iteration progress value, while speed sets the speed that it interpolates towards that value to make it smoother.

## Convert Model to something with a splatter texture

https://www.youtube.com/watch?v=OgCZCd7QV1A

Blender Geometry Node Psuedocode:

**Scatter Points along *existing* curve**:
*# instead I can generate curves using repeat*

Curve => points 
=> delete geometry with random value, boolean, set probability
=> set position, use noise color => math / normalize / scale => offset

**Duplicate those points and set their position by noise**:

=> duplicate elements => set position, same color noise =>scale=>offset
Adjust noise to 4d with Index=>W, adjust scale of set pos of duplicated points
=> points to volume with same voxel and radius size, small
=> volume to mesh

volume => mesh => out

Scattered points =>

Moving hands shows new instances of the paint meshes (eventually figure out how to make this take less memory. For now, use a pool of meshes with reveal set to zero so it isn't rendered.
1. Generic paint mesh. Could be a few dif versions, maybe 5
2. Make paint meshes expand in more than one direction

New idea: hands up down left right equals direction, distance between hands equals how much offset the paint has. Export various models and choose between which to use.

After paint has been left, it slightly grows a bit, then engross and settles before slightly pulsing. Also could  

### The Process / Issues
1. Took points and duplicated them, offset them with noise
2. Remeshed by converting to a volume and back to a mesh
3. The problem: this lost the v_color data so it couldn't be revealed for the "grow" effect in Unity
4. Solution: make it still look paint like without remeshing
    - this took a long time but I made it work
    - duplicate all the points before converting to curves. Store the duplicate as d_index on each set of points
    - offset position of all points using noise
    - create a curve group ID with p_index (how many big curves) multiplied by 1000, with v_index (iterations) added to it as a sub value. 
    - use this Id to create curves, resulting in curves connecting all duplicates for every original iteration (kinda like a bunch of concentric circles growing outwards)
    - use the same ID to randomly delete points, resulting in entire concentric circle to be deleted, thinning it out a bit so its not as connected
    - create another ID using the duplicate index as the major (times 1000) and adding v_index (iterations) to it. Delete points using this, which results in duplicate points for each iteration being randomly deleted. This keeps the curve but makes it shorter, altering the length of each small curve
    - these precise delete sets model the random spread of when I used remeshing.
    - keep the quadrilateral curve to mesh
    - subdivide the surface so its smoother like paint

## Implement New paint texture
- added a particle system that looks like stars for when nothing yet

Pool system that starts them deactivated
Moves them to the location of direction of hands and activates them
Hand speed sets acceleration, hand distance sets target value

Dictionary of VisualObject, targetReveal and
VisualObject, acceleration
VisualObject, currentRevealValue

Update should run, for each VisualObject in VisualObjects2
currentValue = Mathf.Lerp(currentValue, targetValue, acceleration * Time.deltaTime);
material.SetFloat(shaderValueName, currentValue);


- bounding box helps calculate distance,
- default shoulders feet, choose the nose or shoulders, and feet, if time have duyanicay pick higher points and have a scale calculate a distance away. Use a running average to stabilize the distance
- running average for pose stabilization
- if something isn't available, return a very low or large value
-

For pooling: start with over budgeting, later implement scaling pooling system
- eventually look into the Unity Pooling library
- spawn everything in activate it, then update the reveal amount when you move it, 

- make interaction signals work with multiple people
- each pose gets its own interaction signals and its own gesture recognizer
- there's also a combined interaction signals

