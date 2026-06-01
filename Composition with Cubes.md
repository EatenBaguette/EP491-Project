# A Composition with Cubes by Ethan Bessette

## Video Demonstration
https://drive.google.com/file/d/1ErOG_dyi9FcgdT0QbyRR6mBElcRRFTKl/view?usp=sharing

Watch the video to understand what this project is.

## Artist Statement
This piece was inspired by the concepts of multimodality, conceptual art, and aura in a digital era. Multimodality, as described in Essentials of Visual Interpretation (Reynolds and Niedt 10), is the idea that a form of communication that combines multiple avenues of perception is greater than the sum of its parts. Conceptual art was an art style practiced by Sol LeWitt that elevated the concept and instructions of the art creation over the resulting work, similar to a composer's musical score compared with the musicians and conductors who realize it. This idea is exponentiated when combined with that of aura in a digital era. Aura was described by Walter Benjamin: "If, while resting on a summer afternoon, you follow
with your eyes a mountain range on the horizon or a branch which casts its shadow over you, you experience the aura of those mountains (Benjamin 4-21)." In contrast, art reproductions have the ability to bring the art into the space of the viewer rather than traveling out to see the sight in its original form. The interesting thing about digital art is that it has no original form. This piece features multimodality, is a form of conceptual art, and plays with the idea of aura in a digital era.

It features multimodality because it can be "conducted" by the viewer, and the physical interaction is part of the unique experience. The meaning of the piece is able to be understood when the viewer feels the way the subtle movements in their hands affect the forces applied to the cubes in the simulation. Viewing another participate, or watching a video of the piece, detracts from its impact. It is like how when music is paired with visuals, the range of subjective emotions are made much more singular and objective. The feeling of control is a major PART of the piece itself, making it multimodal.


This ability to control the piece in real time, or "conduct" it, is what makes this piece a form of conceptual art. The artwork itself is the coding choices that set boundaries on the interactions. For example, I designed an emission map in Photoshop, but I set the tiling of the map randomly when a cube is instantiated. On impact, the cubes attain a random color before interpolating back to white over time. I created the structure for the simulation, like defining the modes of interaction and specific gestures that adapt the piece. All of these set boundaries on the piece, but don't define exactly how each cube will act in space. That is up to chance and the control of the viewer. The code and choices I made to define the interactions make the piece a form of conceptual art.

Finally, this piece plays with the idea of aura. All digital art does, to some extent, because it is represented abstractly in binary data. Barring computational constraints, a digital artwork can be shown in many locations, bringing it directly to the viewer rather than being bound by a specific location, such as a mountain range. Due to the extent of the randomness in this piece, it especially lacks aura, since not only will the presentation format change each time, but so will the contents of the piece.

This was super fun to create. I especially enjoy the idea of conceptual art and I think I use it often in my work. I'm not the best decision maker; it's much easier for me to define ranges of choices that will be decided for me at runtime.
Thank you for taking the time to understand some of my thoughts about this piece. I will definitely continue working on it!

Works Cited

Benjamin, Walter. The Work of Art in the Age of Mechanical Reproduction. Schocken Books, 1935.

Landay, Dr. Lori. “Conceptual Art and Modern Machine Learning.” Apr. 2026.

Reynolds, Rachel R, and Greg Niedt. Essentials of Visual Interpretation. Taylor & Francis, 29 Dec. 2020.

## How To Use
This piece is a Unity project that can be opened form the Unity Hub by navigating to the Unity Version folder.
 
Once loaded, the project is ready to run in the editor. If it doenst respond, try moving forward or back until your poses are recognized. Additioanlly, you can retrain the poses by selecting VisualController object which has keys mapped for recording training examples, training the model, and entering prediction mode. Before recording examples, make sure to set the intended output gesture index. Currenly, zero is trained as hands down, and 1 is hands up.

## Uses of Machine Learning
1. Pose landmark recognition using the YOLO pose model, trained on the COCO dataset.
2. Dynamic Time Warping (DTW) for gesture recognition. Bassed on the RTML Unity package. It was heavily modified as it did not implement true DTW. 
3. ChatGPT and Junie for code generation.

## How it Works
1. WebCam.cs requests permission to use the webcam. It selects the webcam to use based on the string provided in the inspector. It initializes it and stores the data in a texture.
2. DetectLandmarks.cs converts the texture to a new tensor each frame and sends it to the pose recognizer. The pose recognizer processes it on the GPU, if available. The output is serialized as raw landmark points as well as parsed into a Pose class for each recongized person, containing Keypoint structs for the position and confidence of each landmark. 
3. DetectGestures.cs controls the gesture recognition model. It sends recorded gestures to the model, requests model training, and requests predictions each frame during predict mode, serializing the predicted gesture index.
4. RTMLCore houses the communication with models. DTW is currently selected. It is controlled by DetectGestures. It has the ability to serialize the model weights and biases in JSON format and load from the file.
5. InteractionController.cs polls the predicted gesture index in DetectGestures.cs. It also polls the Keypoints in the first pose in DetectLandmarks.cs. It uses these values to update an InteractionSignals class which is passed to the VisualController class (and later, the AudioController class)
6. The InteractionSignals class holds data like current gesture, hand height, hand distance, etc. This enables realtime parameter control.
6. The VisualController handles visual state changes and instantiates and updates VisualObjects based on the data in InteractionSignals. It serializes state specirfic values that are used by the visual states, such as the Center Repel Force in the Edge Orbit state.
7. Each visual state handles the physics and other visual effect logic, telling the VisualController to update all the VisualObjects in different ways.

## Implementation Process

There were a few phases I went through during implementation, which explains the three project verison (web, python, and Unity).

### Web and python
1. I added pose recongition to the example from the Machine Learning Repository.
2. I added multiple pose recognition so that mutliple people can be tracked.
3. I tried converting this to python to make it run more efficiently, but when I ran into a few issues I decided if it would be difficult anyways, I would rather push towards the Unity version since it would be something I would use more in the future.

### Unity

1. Export YOLO pose recognizer to onnx, import to Unity
2. Follow the webcam documentation combined with help from a coding agent to get the proper permissions to select a camera and use it in Unity.
3. Test the camera in Unity, create a small scene that shows the camera.
4. Write a script to stream the webcam data to the pose recognizer. Format the data in an easy to read way
   - make landmarks into an enum
   - make a struct for keypoints to easily name them in the inspector and pair them with their position
   - make a class pose to hold instances of all the key point structs to make it easier to work with a list of all people recognized
   - create logic to only add a pose to the list if the confidence rating is above a threshold. Send 0s for all key point positions if their confidence is below a threshold.
   - use multiple workers, one to run the model, and the other for everything else
   - convert the webcam data to tensor with shape TensorShape(1, 3, height, width) # batch, RGB, pixel H and W
   - a list of raw landmarks that I studied to understand the format to convert into the pose class and landmark structs I mentioned earlier

5. Implement Dynamic Time Warping
   - I downloaded a package with a useful UI for training models within Unity that saves the weights to a JSON file. It works similar to wekinator, you can add samples, train on the samples, then enter prediction mode
   - It wasn't using actual DTW, so I got help from a coding agent to modify it use use true DTW
   - I tested the training a little and it worked! I didn't save the training though because I was sitting and I want to be standing
   - I later realized that the DTW could not save as JSON format, so I had to change the way the model was serialized.
6. Define state machine structure. I decided to use an InteractionController that would poll the gesture data and pass it as InteractionSignals to the other controllers.
7. Create a basic InteractionSignals class that would hold realtime signals and derived signals such as hand distance.
8. Create a VisualObject class to control each object. Attach to a prefab. In Photoshop, make the emission map so that each cube can appear in the style of Pier Mondrian which is what this piece is named after. 
9. Create a VisualController class to instantiate and control the objects.
10. Create a VisualState abstract class. Implement a number of subclasses that control the visual states, like IdleState and EdgeOrbitState.
11. Connect the InteractionController to the VisualController, send InteractionSignals down the line to each VisualState.
12. Before implementing switching states, test the Orbit hand distance setup.
13. Implement state switching in the VisualController by monitoring the gesture index.
14. Refine the physics in the VisualStates.
15. Add collision detection the objects, fine tuning the friction amount.
16. Add color changes on collision. I had to learn how to acces the shader paramaters on each VisualObject at runtime. I then use Color.Lerp to interpolate the color constantly back to white.
17. Add tiling randomization. I ran into a lot of issues with this.

## What I Learned
- convert a ML model to onnx format for use in unity
- convert a webcam stream to a tensor
- access Shader properties in Unity such as _BaseMap
- parse a list of pose data and determine what each value means through reviewing limted documentation as well as trial and error
- I learned a good format for combining some states with realtime control to create more variation and user control. I will definetly be using this structure in future interactive projects.

## Reflection
Reflection: challenges, unfinished work, and what you would change

- Had trouble using the correct camera
- The DTW model could not be saved in the current way that RTML was serializing the model data.
- tiling randomization. Randomizing the emission map tiling took a lot of troubleshooting. It turns out that the shader I was using did not expose the scale of the emssion map. I ended up usign the _BaseMap which worked!
- The orbiting object went in front of the camera. I used a cone shaped mask that added force to repel them from the center of the viewport.
- I had a lot more issues but I did not remember to document them.
- I want to add audio! I ran out of time.


## All LLM and agent prompts

### Creating easy access to landmark data

Modify the DetectLandmarks.cs and WebCam.cs so that at runtime, the DetectLandmakrs uses the continuously updated WebCamTexture and converts it to an input tensor and runs it through the model chosen in the inspector



Example script using a handwritten digit classification model:
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


———————


Instead of showing the landmarks in the inspector, lets make a list of Vector3 for each keypoint of each detected pose.

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

Allow the list of poses and their keypoints to be shown in the inspector, showing each joint by name using a struct.


——————————————


### Creating Dynamic Time Warping

I plan to use the RTMLToolKit namespace to perform dynamic time warping on pose data. However, I can't think of the best way to handle multiple poses. RTMLCore requires a set input and output size. One method could be to pass 17 * 2 values for each pose in poses. The DTW model would output gestures for each pose in quick succession, however it might not recognize gestures that involve multiple people, like a high five.

The functions to use in the Update() would be RecordSample() and PredictSample(). Whicever method above used, the data needs to be passed to a model using RecordSample(). It also needs to be passed to the model using PredictSample() which triggers when the model is in inference mode. This returns the output, which would have to be exposed so other classes can monitor it.


——————————

Modify the DTW scripts so that it fits the needs for training the model using input from DetectLandmarks.cs
Convert it so that it uses true Dynamic Time warping  by comparing frame sequences rather than single points in time.

——————————

instead of training static poses, i want to train on gestures (sequences of pose frames). allow R to start recording, and S to stop so that it records the whole gesture

—————————

modify the gesture script again
- do not resample all gestures to the same frame count to align with the correct DTW
- remove editor conditional section so that all relavent things are shown in inspector
- replace sphere material output with a single integer output contained in this script. the output size will be 1, and the integer will change based on the gesture index during training
- use the first pose (this will be changed and built upon later)
- if the pose data from DetectLandmarks is null, input zeros for all values (to train on "silence"


—————————

### Defining the state machine structure

The detected gesture index from the DetectGestures class, in addition to the elements of each pose in the DetectLandmarks class, will be used to drive the visuals and audio. Should I have a GameController class that handles monitoring this and then passes the appropriate information to the audio and visual handlers?

VisualController:
- On start, instantiate a number (chosen in the inspector) of VisualObject classes at the center of the screen
  On start the visual objects will be in the center and will repel to hang around the edge of the screen. This will be controlled by the VisualController script
  Should I have a state system for the VisualController that enables ease of different code in different states and helps with transitions? Should I use abstract class or interface?
  I also want to control audio for realtime soundtrack generation controlled by events in the game. Some of the events are tied to visual states, but other changes should happen regardless of visual state. In this case, should I have a separate AudioController class with its own state machine?

Should the visual controller class and audio controller class both monitor what they need, or should a GameController class monitor this and pass the info required to the other controller classes?

——————————

Is it possible to make this generative in a way such that I dont have to define every possible state  and instead different combinations of gestures combine parts of the logic to create many more states than feasible to write

Answer: use a class that communicates realtime parameter controls and do different things in the states based on those values.

——————————
In the Assets/Scripts folder, create a new folder for the following scripts:

In the VisualStateMachine folder,
VisualController
- Owns visual objects
- Owns visual state machine (using abstract classes)
   - to start, it should have methods for start, update, and exit
   - protected VisualState(VisualController controller) { this.controller = controller; }
   - create states: EdgeOrbit, Lines, Squares, Scatter, Collapse
- Applies VisualModifiers from signals
- Updates all VisualObjects during Update()


——————————

Create a basic InteractionSignals class that will house realtime signals and derived signals such as hand distance. Create an instance of this class in InteractionController and update it on update. Send the values of this class to a method that uses the InteractionSignals class as an argument in the VisualController script.


——————————

update an InteractionController MonoBehaviour.

It should:
- have serialized references to DetectGestures and DetectLandmarks
- have serialized references to VisualController and AudioController, but these can be optional for now
- own one InteractionSignals instance
- expose the current InteractionSignals through a public property
- in Update(), reset frame flags, read the current gesture index, detect gesture changes, update the signals, clamp them, and decay them
- log gesture changes for debugging

——————————

Add simple gesture-to-signal mapping to InteractionController.
Update GestureSO to include values for the signals in InteractionSignal.

When a gesture change is detected:
- the gesture index should find the GestureSO with that index and add its values to the InteractionSignal.

Use additive influence values, then clamp signals to 0..1.
The signals should decay over time, so combinations can overlap.

—————————
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
- avoid null reference errors

———————————

Create a VisualObject MonoBehaviour.

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

——————————
Update InteractionController so that after it updates InteractionSignals each frame, it passes the signals to VisualController if one is assigned.

Call:
visualController.ApplySignals(Signals);

———————————
Goal: adjust forces to be 3 dimensional for VisualObject. implement the EdgeOrbitState. I already adjusted the Gesture SOs to have zero for all values. I also set all the forces on the VisualController to zero. I want to reimplement using states and I will slowly add in InteractionSignals when I am ready to.

- disable camera clamping requirement. The objects might pass slightly out of frame while they orbit
- adjust VisualObjects so they appear more like objects floating in space with smooth motion
- EdgeOrbitState
  : On enter, the world origin becomes a source of gravity for the VisualObjects
  : Set the target position of each VisualObject to a random place in orbit around the origin
  : use minimum and maximum distance from origin fields that can be changed in the inspector
  : use random velocities within a range that can be adjusted
  : On Tick, move VisualObjects towards position in orbit, and once it reaches the position, continue to orbit smoothly around world origin


—————————


### Specifying camera input

how to change which camera is used, its currently using OBS virtual camera instead of the built in webcam

I ended up looking up the list of devices using FFmpeg and using the string listed there as my preferred camera. Eventually I would like to show a list of options to the user, but for now it's hardcoded.


——————————

### Refining the orbit system

what can i do to make sure the objects continue to orbit?

——————————

Add a continuous orbit force in the EdgeOrbitState. Additionally, add small forces with random directions so that the objects do not all amass.

——————————

add a repel mask so that the objects are repelled from the cylinder at the center of the viewport (with repel radius in the inspector)


———————————

update the mask so that it follows the angle of the camera viewport as the depth increases


———————————

### Adding collision logic

How do I access the color and tiling of the material on each VisualObject at runtime?

———————————

### Modifying the JSON model saving format so that Dynamic Time Warping templates can be saved and read by the model

RTML uses input landmark data to train a dynamic time warping model. It is supposed to save the weights and biases and other needed information in a json file.

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

Your task is to make sure the dynamic time warping data created during training is able to be saved

—————————————

### Update visual states

Update VisualController to create inspector check boxes for start mode.
- center start: the current settings to spawn VisualObjects
- field start: spawn VisualObjects at random positions in a cube shape with side lengths of maxOrbitDistance with randomized z positions based on inspector fieldStartDepth. The z values will be based on current z spawn origin plus or minus fieldStartDepth


————————————

Create a visual state IdleState
- On enter, VisualObjects’ new point of gravity is their current position.
- Add the IdleState gravity field to VisualController at serialize it
- On update, add random wander force as well as gravity force so they float in place with some variation

————————————

### Adding movement energy to signals

Modify InteractionController so that it updates the MovementEnergy field of InteractionSignals equal to the change in hand distance