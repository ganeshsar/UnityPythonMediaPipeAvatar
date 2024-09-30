# JIP 2024 Ethereal Matter

## Installation
In order to run the project Python and Unity need to be installed on the device. Other than that, Mediapipe needs to installed which can be done using pip:
```
pip install mediapipe
```

If you don't have git installed, you can install it from [here](https://git-scm.com/downloads). The project can then be cloned (or downloaded):
```
git clone https://github.com/ihendrikson/JIP2024-Mediapipe.git
```

## Running the project
In order to connect Mediapipe to Unity, a python script needs to run on the local sevice which will send the estimated pose over a UDP server to unity.
To run the Python script:
```
cd JIP2024-Mediapipe
cd mediapipeavatar
python main.py
```

You can then open the project and Unity. This can be done by opening the `UnityMediaPipeAvatar` directory. You can open the calibration scene under `Assets/Scenes/CalibrationScene`
