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

The main issue was that to "grow" the model in realtime, the entire mesh has to be allocated in memory, meaning I have to find a different way to animate the geometry repetitions. Instead, the goal is to store a float percentage of total repetitions (repetition progress) in the R value of a color vector on each vertex. In unity Shader Graph, this value can be used to show or hide the geometry.

Update: I downloaded the package from this website (https://github.com/atteneder/glTFast) for converting glb files to prefabs. However, the vertex color data didn't work. I tested it by making a shader graph with the vertex color connected to base color. It should have been a gradient from black to red but it was all gray.

Update: It turns out the problem was during export from Blender. I was correct in using vertex colors since they can easily be read by ShaderGraph. The issue was that I had to select the attribute to export into the vertex colors in the export window. There was a dropdown menu with different ways to set the vertex colors, and I had to select By Name and type the name of the attribute I stored in the geometry nodes (which I called v_color).

I used a small number of points (6) to determine that the iteration process was working properly (tested by setting the base color to the vertex color, which should gradient from black to red as the iterations progress). It worked! I then recreated the final geometry and exported it. I created a shader that sets the alpha value based on the iteration progress and I was able to slide the default value and see the geometry grow!

Next, I created a basic slider UI with target value and speed. The target value sets the target iteration progress value, while speed sets the speed that it interpolates towards that value to make it smoother.
