This document describes my capstone project for the electronic production and design major. It serves as description of each feature in the framework, a record of my thought processes and design decisions, and LLM prompts.

For a video explanation and demo of what the framework might be used for, you can follow this link: https://drive.google.com/file/d/1CxQw6U6A0qhkh7hJm37kcl2lyk5wIwk4/view?usp=sharing

# AI Disclaimer
It's impossible to dispute the negative environmental effects of server farms. The computers that large ML models run on consume so much water for cooling that they destroy the local community. Excessive use of these models can also lead to cognitive decline––"outsourcing your thinking" as some have called it. I have been programming for 1-2 years. It would take decades of experience to have the intuition required to quickly complete project ideas I have. I know my peers are adopting this technology, and I would feel left behind without using it. So I try to learn as I go. My prompts are often as many or more words than the changes to the code that result. I read the documentation for the libraries I'm working with to know what's possible. I meticulously plan the logic and design choices I want to implement, leaving less room for the machine to do my thinking for me. I proofread and customize every output. It may result in less efficient code, but due to the modern speed of computers, my trade-off lies in getting it to work now and knowing efficiency will come with years of experience. I still feel ambivalent about this practice and do my best to offset the costs of my actions, such as by using Ecosia's LLM which runs various versions of Mistral AI on their own 100% renewable energy-powered servers.

# Overview

In the past, the creative use of motion tracking has been limited by the requirement of physical controllers or less accessible sensors, such as lidar. This limitation is vanishing due to the increasing accessibility of powerful GPUs and basic camera sensors, such as those found in many smartphones and computers. This summer I completed my capstone project for the Electronic Production and Design major at Berklee College of Music. The project focused on creating an accessible framework for the deployment of gesture-controlled audio-visual installation artwork. It only requires a computer and a camera. In short, it is capable of translating peoples' movements and gestures into meaningful changes to music and visuals with limited hardware.

First, it uses computer vision to convert each frame of the camera into a tensor, which is processed by the model to predict the location of points on people's bodies, such as nose, left hand, right hand, etc. Run continuously, it gives a messy stream of data describing the predicted locations of each point on every person in frame. After cleaning the data, it can be used to drive the audio and visual engines: for example, moving the hands could paint across the screen, growing shapes with fractal noise based on the distance between the hands while creating musical chords based on the height of the hands.

While this realtime movement tracking is great for changes based on single keypoints, it needed a way to detect patterns in combinations of keypoints. The framework also implements a dynamic time warping (DTW) classification model to detect these patterns, called gestures. This is better for finite actions, like jumping. Unlike the location of the nose, a jump doesn't last forever. Dynamic time warping can recognize the gesture independent of variations in its length or position. If you jump higher, you might be in the air longer than a hop, but it's still a jump. If you're a musician, think of it like musical contour. A motif can go up and down in a specific pattern, and variations might follow the same pattern with slightly different notes, but we know they're related. The DTW model does this with all the points of the body. Slight variations in position will still be recognized.

For a video explanation and demo of what the framework might be used for, you can follow this link: https://drive.google.com/file/d/1CxQw6U6A0qhkh7hJm37kcl2lyk5wIwk4/view?usp=sharing

## Requirements

Software:  Unity 6000.3.7f1, Audiokinetic Wwise 2024.1.9.8920

Hardware: a computer and a camera

Hardware currently used: a computer running MacOS silicon with 36 GB of memory and a built-in webcam.

Additional software used: Blender 5.0.0

# Features/How it works

1. WebCam.cs requests permission to use the webcam. It selects the webcam to use based on the string provided in the inspector. It initializes it and stores the data in a texture.
2. DetectLandmarks.cs converts the texture to a new tensor each frame and sends it to the pose recognizer. The pose recognizer processes it on the GPU, if available. The output is serialized as raw landmark points as well as parsed into a Pose class for each recognized person, calculating the center position of each pose and listing keypoint structs containing the position and confidence of each landmark. 
3. DetectGestures.cs controls the gesture recognition model. It sends recorded gestures to the model, requests model training, and requests predictions each frame during predict mode, serializing the predicted gesture index.
4. RTMLCore houses the communication with models. DTW is currently selected. It is controlled by DetectGestures. It has the ability to serialize the model weights and biases in JSON format and load from the file.
5. InteractionController.cs polls the predicted gesture index in DetectGestures.cs. It also polls the poses in DetectLandmarks.cs. It uses these values to update an InteractionSignals class which is passed to the VisualController class and AudioController class.
6. The InteractionSignals class holds data like current gesture, hand height, hand distance, etc. This enables realtime parameter control. InteractionController also handles decaying the values in the signals class so that they return to default values when motion has stopped. It also references Gesture scriptable objects that can add to values in the InteractionSignals class when specific gestures are recognized.
7. The VisualController handles visual state changes and instantiates and updates VisualObjects based on the data in InteractionSignals. It serializes state specific values that are used by the visual states, such as the Center Repel Force in the Edge Orbit state. When adding a new VisualState, VisualController should be updated to contain required serialized values, and the VisualState should pull from the controller.
8. Each visual state handles the physics and other visual effect logic, telling the VisualController to update all the VisualObjects in different ways.
9. All VisualObjects and its inheritors contain references to values specific to them, like certain shaders, etc. If a VisualState uses a certain VisualObject prefab, it should update values based on which are available in the VisualObject.
9. The AudioController handles all audio state changes and sends information to Wwise based on the data in interaction signals. It also serializes values specific to the AudioStates. It currently holds references to Wwise events, though in the future all wwise event references may be moved to a separate class. It also references a midi player class.
10. The WwiseMidiNoteSetPlayer references a scriptable object MidiNoteSetDatabase, which in turn stores implementations of the MidiNoteSetDefinition scriptable object. The WwiseMidiNoteSetPlayer chooses a set of notes to play from based on the value of a Wwise switch. It tracks active notes, ending them when a new batch of notes is started. It also tracks manually activated note on and off messages. It posts the necessary midi events to Wwise. The MidiNoteSetDefinition implementations have an attached Wwise switch ID value which is stored in the MidiNoteSetDatabase so a specific note set implementation is chosen based on the switch ID value. From there, they contain lists of midi notes and octave offsets with attached Wwise switch IDs to control chord voicing within each note set.

# Implementation Process

## Export YOLO pose recognizer to onnx, import to Unity

Documentation from this website was used: https://docs.ultralytics.com/modes/export

The yolo CLI was downloaded in order to run the yolo export command.

Note that the device must match the architecture. In this case, I used device=mps since I'm running Mac Silicon. To build for other platforms, I would need to export multiple models and select which to use based on platform in Unity.

Importing to Unity is as easy as dropping in the onnx file.


## Accessing a camera

The example script from the documentation was used:
https://docs.unity3d.com/ScriptReference/WebCamTexture.html

Additions include printing a list of cameras to the console, then using a serialized string of camera name to set the camera.

Also, rendering to a quad and setting the size of the quad based on the aspect ratio.

## DetectLandmarks

Prompt:

"Write a script called DetectLandmarks to use the continuously updated WebCamTexture and convert it to an input tensor which is run through a model chosen in the inspector
   - use multiple workers, one to run the model, and the other for everything else
   - convert the webcam data to tensor with shape TensorShape(1, 3, height, width) # batch, RGB, pixel H and W

Example script using a digit classification model:
```
public class ClassifyHandwrittenDigit : MonoBehaviour
{
public Texture2D inputTexture;
public ModelAsset modelAsset;

Model runtimeModel;
Worker worker;
public float[] results;

void Start()
{
Model sourceModel = ModelLoader.Load(modelAsset);

// Create a functional graph that runs the input model and then applies softmax to the output.
FunctionalGraph graph = new FunctionalGraph();
FunctionalTensor[] inputs = graph.AddInputs(sourceModel);
FunctionalTensor[] outputs = Functional.Forward(sourceModel, inputs);
FunctionalTensor softmax = Functional.Softmax(outputs[0]);

// Create a model with softmax by compiling the functional graph.
graph.AddOutput(softmax);
runtimeModel = graph.Compile();

// Create input data as a tensor
using Tensor<float> inputTensor = new Tensor<float>(new TensorShape(1, 1, 28, 28));
TextureConverter.ToTensor(inputTexture, inputTensor);

// Create an engine
worker = new Worker(runtimeModel, BackendType.GPUCompute);

// Run the model with the input data
worker.Schedule(inputTensor);

// Get the result
Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;

// outputTensor is still pending
// Either read back the results asynchronously or do a blocking download call
results = outputTensor.DownloadToArray();

// Release outputTensor memory
outputTensor.Dispose();
}

void OnDisable()
{
// Tell the GPU we're finished with the memory the engine used
worker.Dispose();
}
}
```
"


This output a list of 17000 or so values. The online documentation lists the order that keypoints are listed. I determined that 300 poses are recognized. Based on this, I 
   - made a struct for keypoints to easily name them in the inspector and pair them with their position
   - made a class pose to hold instances of all the key point structs to make it easier to work with a list of all people recognized
   - created logic to only add a pose to the list if the confidence rating is above a threshold. Send 0s for all key point positions if their confidence is below a threshold (serialized)

Prompt:

"Instead of showing the landmarks in the inspector, lets make a list of Vector3 for each keypoint of each detected pose.

Every 57 indices, another pose is shown (up to 300).
The first 4 indices of each pose represents x, y, x, confidence, followed by the 5th index which is always 0. Then, the following values represent the x, y, and confidence of each landmark (17 total). For example, landmarks[6] is the x coordinate from the upper right of the nose of the first pose, landmarks[64] is the y coordinate from the upper right of the nose of the second pose.

Create a list called poses.
For each pose confidence, for example, landmarks[4], landmarks[61], etc. If the pose confidence is less than 0.5, remove it from the list. If it is above 0.5, add it to the list.

In each pose of poses, store a list of vector2 of keypoints following this list of names, assigning indexes to each:
Nose // this would be index 0
Left Eye
Right Eye
Left Ear
Right Ear
Left Shoulder
Right Shoulder
Left Elbow
Right Elbow
Left Wrist
Right Wrist
Left Hip
Right Hip
Left Knee
Right Knee
Left Ankle
Right Ankle

If the confidence of any of the keypoints is less than 0.5, use zero for x and y.

Serialize the list of poses and their keypoints using a list of keypoint structs for each pose."


## DetectGestures

Implementing Dynamic Time Warping
   - I downloaded a package with a useful UI for training models within Unity that saves the weights to a JSON file. It works similar to wekinator, you can add samples, train on the samples, then enter prediction mode
   - It wasn't using actual DTW, so I got help from a coding agent to modify it use use true DTW
   - I later realized that the DTW could not save as JSON format, so I had to change the way the model was serialized.

Prompt:

"Modify the DTW scripts so that it fits the needs for training the model using input from DetectLandmarks.cs
Convert it so that it uses true Dynamic Time warping by comparing frame sequences rather than single points in time.

instead of training static poses, I want to train on gestures (sequences of pose frames). R to start recording, and S to stop so that it records the whole gesture

- do not resample all gestures to the same frame count 
- serialize all relevant values
- replace output to a single integer output contained in this script. the output size will be 1, and the integer will change based on the gesture index during training
- use the first pose (this will be changed and built upon later)
- if the pose data from DetectLandmarks is null, input zeros for all values (to train on "silence"
"

## State machine

Thought process: how can I use the least number of states and easily create variations based on the combination of values from different order of gestures? Detected gestures could stack values that return to default, resulting in unique combinations of values at any time.

I decided to use an InteractionController that would poll the gesture data and pass it as InteractionSignals to the other controllers. The other controllers would use abstract classes.
The InteractionSignals class that holds realtime signals and derived signals such as hand distance, and gesture index.
I used scriptable objects for each pose that can update the values in InteractionSignals.

Prompt:

"In the Assets/Scripts folder, create a new folder for the following scripts:

In the VisualStateMachine folder,
VisualController
- Owns visual objects
- Owns visual state machine (using abstract classes)
   - to start, it should have methods for start, update, and exit
   - protected VisualState(VisualController controller) { this.controller = controller; }
   - create states: EdgeOrbit, Lines, Squares, Scatter, Collapse
- Applies VisualModifiers from signals
- Updates all VisualObjects during Update()
"

"update an InteractionController MonoBehaviour.

It should:
- have serialized references to DetectGestures and DetectLandmarks
- have serialized references to VisualController and AudioController, but these can be optional for now
- own one InteractionSignals instance
- expose the current InteractionSignals through a public property
- in Update(), reset frame flags, read the current gesture index, detect gesture changes, update the signals, clamp them, and decay them
- log gesture changes for debugging
"Create an instance of this class in InteractionController and update it on update. Send the values of this class to a method that uses the InteractionSignals class as an argument in the VisualController script."


"Add simple gesture-to-signal mapping to InteractionController.
Update GestureSO to include values for the signals in InteractionSignal.

When a gesture change is detected:
- the gesture index should find the GestureSO with that index and add its values to the InteractionSignal.

Use additive influence values, then clamp signals to 0..1.
The signals should decay over time, so combinations can overlap.

Extend InteractionController to compute basic pose-derived signals from DetectLandmarks.

Add:
- HasPose
- LeftHandPosition
- RightHandPosition
- BodyCenter
- HandDistance normalized 0..1
- AverageHandHeight normalized 0..1

Keep the calculations defensive:
- handle missing pose data
- handle missing keypoints
- avoid null reference errors"

"Create a VisualObject MonoBehaviour.

It should:
- store velocity
- have ApplyForce(Vector2 force)
- have Tick(float deltaTime)
- move in 2D space
- optionally clamp or wrap around camera bounds

——————————

Update a VisualController MonoBehaviour.

It should:
- have a VisualObject prefab reference
- have an object count set in the Inspector
- instantiate that many VisualObjects at the center of the screen on Start
- store them in a list
- expose ApplySignals(InteractionSignals signals)

Update InteractionController so that after it updates InteractionSignals each frame, it passes the signals to VisualController if one is assigned.

Call:
visualController.ApplySignals(Signals);"



## Modifying the JSON model saving format so that Dynamic Time Warping templates can be saved and read by the model

I truly did not know the first thing about saving DTW templates. The weights and biases were serialized into a JSON seemingly ok, but for some reason it was not working with the gesture templates.

Prompt:

"RTML uses input landmark data to train a dynamic time warping model. It is supposed to save the weights and biases and other needed information in a json file.

I trained during runtime, then entered prediction mode and it worked.
- During runtime, the training process functions.
- After which the prediction process functions.

However, the model is not able to be saved (none of the templates from DTW are saved). Upon exiting and re-entering play mode, the model must be re-trained. The json file is empty. These errors appear in the console:
[DTWRecognizer] No templates available to predict from.
UnityEngine.Debug:LogWarning (object)
RTMLToolKit.Logger:LogWarning (object) (at Assets/RTMLToolKit/Util/Logger.cs:17)
RTMLToolKit.DTWRecognizer:Predict (single[]) (at Assets/RTMLToolKit/Core/DTWRecognizer.cs:100)
RTMLToolKit.RTMLCore:PredictSample (single[]) (at Assets/RTMLToolKit/Core/RTMLCore.cs:429)
DetectGestures:UpdateGesturePrediction (single[]) (at Assets/Scripts/DetectGestures.cs:188)
DetectGestures:Update () (at Assets/Scripts/DetectGestures.cs:135)

Your task is to make sure the dynamic time warping data created during training is able to be saved"

## Blender to Unity

I explored geometry nodes in blender, but exporting to Unity was tough.


The main issue was that to "grow" the model in realtime, the entire mesh has to be allocated in memory, meaning I have to find a different way to animate the geometry repetitions. Instead, the goal is to store a float percentage of total repetitions (repetition progress) in the R value of a color vector on each vertex. In unity Shader Graph, this value can be used to show or hide the geometry.

Update: I downloaded the package from this website (https://github.com/atteneder/glTFast) for converting glb files to prefabs. However, the vertex color data didn't work. I tested it by making a shader graph with the vertex color connected to base color. It should have been a gradient from black to red but it was all gray.

Update: It turns out the problem was during export from Blender. I was correct in using vertex colors since they can easily be read by ShaderGraph. The issue was that I had to select the attribute to export into the vertex colors in the export window. There was a dropdown menu with different ways to set the vertex colors, and I had to select By Name and type the name of the attribute I stored in the geometry nodes (which I called v_color).

I used a small number of points (6) to determine that the iteration process was working properly (tested by setting the base color to the vertex color, which should gradient from black to red as the iterations progress). It worked! I then recreated the final geometry and exported it. I created a shader that sets the alpha value based on the iteration progress and I was able to slide the default value and see the geometry grow!

Next, I created a basic slider UI with target value and speed. The target value sets the target iteration progress value, while speed sets the speed that it interpolates towards that value to make it smoother.


## Making the geometry look more paintlike

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


I then tested in Unity and it worked. I added logic to the ShaderGraph to color the object differently based on its reveal amount.