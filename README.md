# LeiaUltraleapUnitySample

## Overview
This project showcases a simple model viewer leveraging both Leia's and Ultraleaps's Unity Plugin to demonstrate interactive controls. 

Users can scale and rotate a target object using pinch gestures with Ultraleap's hand tracking technology on Leia's Spatial Reality Display (SRDisplay). This integration creates an immersive user experience by enabling intuitive 3D interaction.


![image](https://github.com/LeiaInc/LeiaUltraleapUnitySample/assets/51935243/4a3abbdf-6845-461c-bf36-46cd65ce3487)


### Using Existing Sample Scene

This project uses the Model Viewer Sample Scene found in our plugin at 

 **Assets/Leia/Examples/ModelViewer**
 
 For those looking for simple Windows and Android integration without Ultraleap, we suggest starting there.
Documentation can be found here:

https://support.leiainc.com/developer-docs/unity-sdk/leia-unity-plugin-guide/extensions/model-viewer-sample-scene

## Prerequisites

SR Platfrom v1.28, downloadable here:

https://www.srappstore.com/


This project is developed with specific versions of the Leia Unity SDK and Ultraleap Unity Plugin to ensure compatibility and functionality. Ensure you have the following setup:

- [LeiaUnitySDK v3.2.3](https://www.leiainc.com/developer-resources) - Download and import this SDK into your Unity project.
- [Ultraleap.UnityPlugin v6.14.0](https://github.com/ultraleap/UnityPlugin/releases) - Download and import this plugin into your Unity project.

Note: The LeiaUnitySDK v3.2.3 and Ultraleap.UnityPlugin-6.14.0 are already integrated into the project.

## Getting Started

### Installation
To get started with the LeiaUltraleapUnitySample project, follow these steps:

1. Clone this repository to your local machine.
2. Open the cloned project in Unity.

There is no need to import the LeiaUnitySDK or Ultraleap.UnityPlugin packages as they are already configured within the project.

### Running the Sample

Follow these steps to explore and interact with the sample scene:

1. **Open the Scene**: In Unity, navigate to the `Scenes/LeiaUltraleapSampleScene` and double-click it to open.

2. **Launch the Scene**: Press the Play button in Unity. This action starts the scene, allowing you to interact with the model using hand gestures.

3. **Interacting with the Model**:
   - In the Hierarchy window, find the `Model Viewer` object and expand it to see its children, including the `Model Pivot` and `LeiaLogo`.
   - To understand how interactions are managed, look for the `UltraleapScaleRotate` script attached to relevant game objects. This script facilitates the scaling and rotation of the model through pinch gestures.

## Features
The project includes key features that demonstrate the capabilities of combining Leia's display technology with Ultraleap's hand tracking:

- **Object Scaling:** Users can apply pinch gestures to adjust the size of the target object, making it larger or smaller.
- **Object Rotation:** By moving their hands while pinching, users can rotate the target object, exploring it from different angles.

## Support
Should you face any challenges or have questions regarding the project, the Leia Developer Resources provide extensive documentation and support options:

- [Leia Unity Plugin Guide](https://support.leiainc.com/developer-docs/unity-sdk/leia-unity-plugin-guide)

