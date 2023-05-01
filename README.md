# Code for "Experimental Digital Twin for Job Shops with Transportation Agents" (EDT for JSPTA)
[[Full Paper]](PLACEHOLDER)

Code for "Experimental Digital Twin for Job Shops with Transportation Agents" Research Paper. 

<p align="center">
   <img src="https://github.com/aymengan/EDT_JSPTA/blob/main/media/Movable%20Camera%20(2).PNG"/>
</p>

##### Contents  
- [Introduction](#intro)  
- [Installation](#install)
- [Usage](#usage)
- [Design and Customization](#dc)
   

<a name="intro"/>

## Introduction
Production scheduling in multi-stage manufacturing environments is subject to combinatorial optimization problems, such as the Job Shop Problem (JSP). The transportation of materials when assigned to mobile agents, such as Automated Guided Vehicles (AGVs), results in a Job Shop Problem with Transportation Agents (JSPTA). The transportation tasks require routing the AGVs within the physical space of the production environment. Efficient scheduling of production and material flow is thus crucial to enable flexible manufacturing systems.

Neural combinatorial optimization has evolved to solve combinatorial optimization problems using deep Reinforcement Learning (RL). The key aim is to learn robust heuristics that tackle the trade-off of optimality versus time complexity and scale better to dynamic changes in the problem. The present simulation environments used to train RL agents for solving the JSPTA lack accessibility (e.g. use of proprietary software), configurability (e.g. changing shop floor layout), and extendability (e.g. implementing other RL methods).

This research aims to address this gap by designing an Experimental Digital Twin (EDT) for the JSPTA. It represents an RL environment that considers the physical space for the execution of production jobs with transportation agents. We created our EDT using a simulation tool selected based on requirement analysis and tested it with a customized state-of-the-art neural combinatorial approach against two common Priority Dispatching Rules (PDRs).

With a focus on the makespan, our findings reveal that the neural combinatorial approach outperformed the other PDRs, even when tested on unseen shop floor layouts. Furthermore, our results call for further investigation of multi-agent collaboration and layout optimization. Our EDT is a first step towards creating self-adaptive manufacturing systems and testing potential optimization scenarios before transferring them to real-world applications.

<p align="center">
   <img src="https://github.com/aymengan/EDT_JSPTA/blob/main/media/Layout%203.gif" width="600" height="340"/>
</p>

<a name="install"/>

## Installation
This repository contains an Experimental Digital Twin (EDT) of a Job Shop with Transportation Agents (JSPTA) present in flexible manufacturing systems. Prior knowledge of the use of Unity is recommended.
The following software is required:
- [Unity Editor 2021.3.9f1](https://unity.com/)
- [Unity ML-Agents Toolkit](https://github.com/Unity-Technologies/ml-agents)

To properly setup the environment you will need to follow the next steps:
1. Install the [Unity Editor 2021.3.9f1](https://unity.com/)
2. Clone the repository using ```git clone https://github.com/aymengan/EDT_JSPTA.git```
3. Open the Environment folder of this repository as a project with the Unity editor
4. Setup a python environment with [Unity ML-Agents Toolkit](https://github.com/Unity-Technologies/ml-agents)

Note: Given size limitations the packages contained within Unity project were removed, nonetheless, once it is opened with Unity the packages will download automatically. 

<a name="usage"/>

## Usage

The repository includes one sample of the JSPTA configurations used within the original work. The configuration is refered to as 6x3x2_L3, meaning the third layout configuration of the JSPTA problem with 6 jobs, 3 workstations and 2 agents. To implement costum configurations please refer to [Desing and Customization](#dc). 

<p align="center">
   <img src="https://github.com/aymengan/EDT_JSPTA/blob/main/media/Direct%20Play.PNG"/>
</p>

The repository comes with already trained agents for the JSPTA, you can directly press play to see how the agents solve the JSPTA problem. Within the FAS GameObject under the FASInfo script the user can change the random seeds used for the creation of the JSP problem. The jobs of the JSP are created using an analogous method to the [Taillard](http://dx.doi.org/10.1016/0377-2217(93)90182-M) benchmarks. Consequently, there are two seeds, the job seed and the machine seed. These agents were trained with a job seed of 100 and a machine seed of 150. For more information about the underlying behaviors of the environment please refer to the [original paper](#og). 

<p align="center">
   <img src="https://github.com/aymengan/EDT_JSPTA/blob/main/media/Random%20Seeds.PNG"/>
</p>

The environment comes with 2 implemented Priority Dispatching Rules (PDRs); Longest Processing Time (LPT) and Shortest Processing Time (SPT). They can be toggled on and off within the AGV GameObjects. Multiple AGVs within the system can use different heuristics or trained networks. 

<p align="center">
   <img src="https://github.com/aymengan/EDT_JSPTA/blob/main/media/Behavior%20Types.PNG"/>
</p>

Note: when using Heuristics and manual control be sure to change the AGV GameObject Behavior Parameters:Behavior Type to "Heuristic Only"

### Training the Agents with ML-Agents Toolkit
Training agents can be easily done using Unity ML-Agents Toolkit. The original configuration file "configuration.yaml" used to train all our agents is included within the repository. To do so follow the next steps:
1. Setup your python environment with a working version of [Unity ML-Agents Toolkit](https://github.com/Unity-Technologies/ml-agents)
2. Open the python command line
3. Run ```mlagents-learn "./configuration.yaml" --run-id sample_JSPTA --time-scale 10```
4. Once ML-agents is ready, click the play button within the environment

We recommend creating an executable and training with no graphics to increase the speed of training and the number of environments that can run in parallel. Within our work we used 4 environments running in parallel with each containing 25 copies of the JSPTA EDT. A sample training environment is shown in the next figure.

<p align="center">
   <img src="https://github.com/aymengan/EDT_JSPTA/blob/main/media/Training_Env.PNG"/>
</p>

The resulting training command will look something like this:
```
mlagents-learn "./configuration.yaml" --run-id sample_JSPTA --env="./path_to_executable" --no-graphics --num-envs 4 --time-scale 10
```

To recreate the whole set of experiments performed within the original work, the JSPTA configurations must be recreated. To do so refer to the next section [Design and Customization](#dc) and the Layouts folder of this repository. Additionally, the 5 JSP instances used within the paper were created using the following job seed - machine seed pair:

- Instance 0 (I.0): 0 - 50
- Instance 1 (I.1): 100 - 150
- Instance 2 (I.2): 200 - 250
- Instance 3 (I.3): 300 - 350
- Instance 4 (I.4): 400 - 450



<a name="dc"/> 

## Design and Customization

Creating a new layout is easy due to Unity's Prefabs. All the elements of the JSPTA EDT are implemented within the environment as Prefabs. To create a new layout follow the next steps:

1. Create a copy of the "sample JSPTA" scene and remove the elements within the FAS GameObject
2. Insert a new Shop Floor and resize to the desired dimensions.

<p align="center">
   <img src="https://github.com/aymengan/EDT_JSPTA/blob/main/media/s2.PNG"/>
</p>

3. Insert the desired Geofences.

<p align="center">
   <img src="https://github.com/aymengan/EDT_JSPTA/blob/main/media/s3.PNG"/>
</p>

4. Insert the Workstations of the JSPTA. Assign a unique ID to each Workstation greater than 0. Add a custom NavMesh Obstacle to the Workstations to set the distance with which the agents will try to avoid collisions with workstations. Within our work we used a $2x4x1.5$ Obstacle with Carve option and our workstations were scaled to $0.5x0.3x0.5$.

<p align="center">
   <img src="https://github.com/aymengan/EDT_JSPTA/blob/main/media/s4.PNG"/>
</p>

5. Insert the Products and Delivery Station of the JSPTA. Every Product and Delivery Station must have a unique ID greater than 0.

<p align="center">
   <img src="https://github.com/aymengan/EDT_JSPTA/blob/main/media/s5.PNG"/>
</p>

6. Insert the AGVs. Every AGV must have a unique ID starting with 0. You must reference the MultiAgent controller to the AGVs under JSSP Multi Agent:Controller.

<p align="center">
   <img src="https://github.com/aymengan/EDT_JSPTA/blob/main/media/s6.PNG"/>
</p>

7. Run the environment.

<p align="center">
   <img src="https://github.com/aymengan/EDT_JSPTA/blob/main/media/S7.PNG"/>
</p>

Note: Be sure to Bake the Surfaces with NavMesh and, if using directly for inference, remember to adjust the input and output sizes of the network.  Given a configuration with n products, m workstations and k agents, the input size and output size are $2n+2m(n+1)+k-1$ and $n+1$ respectively.

