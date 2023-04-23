# Code for "Experimental Digital Twin for Job Shops with Transportation Agents" (EDT for JSPTA)
[[Full Paper]](PLACEHOLDER)

Accompanying code for "Experimental Digital Twin for Job Shops with Transportation Agents" Research Paper. 

##### Contents  
- [Introduction](#intro)  
- [Installation](#install)
- [Usage](#usage)
- [Result Reproduction](#repro)
- [Design and Customization](#dc)
   

<a name="intro"/>

## Introduction
<p align="center">
   <img src="https://github.com/aymengan/EDT_JSPTA/blob/main/media/Layout%203.gif" width="600" height="340"/>
</p>

Production scheduling in multi-stage manufacturing environments is subject to combinatorial optimization problems, such as the Job Shop Problem (JSP). The transportation of materials when assigned to mobile agents, such as Automated Guided Vehicles (AGVs), results in a Job Shop Problem with Transportation Agents (JSPTA). The transportation tasks require routing the AGVs within the physical space of the production environment. Efficient scheduling of production and material flow is thus crucial to enable flexible manufacturing systems.

Neural combinatorial optimization has evolved to solve combinatorial optimization problems using deep Reinforcement Learning (RL). The key aim is to learn robust heuristics that tackle the trade-off of optimality versus time complexity and scale better to dynamic changes in the problem. The present simulation environments used to train RL agents for solving the JSPTA lack accessibility (e.g. use of proprietary software), configurability (e.g. changing shop floor layout), and extendability (e.g. implementing other RL methods).

This research aims to address this gap by designing an Experimental Digital Twin (EDT) for the JSPTA. It represents an RL environment that considers the physical space for the execution of production jobs with transportation agents. We created our EDT using a simulation tool selected based on requirement analysis and tested it with a customized state-of-the-art neural combinatorial approach against two common Priority Dispatching Rules (PDRs).

With a focus on the makespan, our findings reveal that the neural combinatorial approach outperformed the other PDRs, even when tested on unseen shop floor layouts. Furthermore, our results call for further investigation of multi-agent collaboration and layout optimization. Our EDT is a first step towards creating self-adaptive manufacturing systems and testing potential optimization scenarios before transferring them to real-world applications.



This repository contains an Experimental Digital Twin (EDT) of a Job Shop with Transportation Agents (JSPTA) present in flexible manufacturing systems.
- What is an FMS and its components (importance of Transportation)
- Target Criteria makespan
- Highly configurable EDT, the creation of arbitrary configurations
- Primary intention was to be RL training environment to test RL solutions for the AGV task allocation
- Simple PDRs implemented 
- Current SotA approach adapted
- Intention to be a JSPTA solution tester and developer tool

## Tools and Methods Used
The EDT was created using the [Unity](https://unity.com/) simulator and the [Unity ML-Agents Toolkit](https://github.com/Unity-Technologies/ml-agents). 
- Main FMS components done through Unity Prefabs. 
- NavMesh package. 
- [Taillard](https://doi.org/10.1016/0377-2217(93)90182-M) JSP Generation
- PDRs - SPT and LPT (explain)
- [Learning to Dispatch](https://doi.org/10.48550/arXiv.2010.12367) Adjustments were made

## EDT Components
- **simple components (image + behavior)** + aesthetics were not taken into account.
- Shop Floor
- Geofences
- Delivery Stations
- Workstations 
- Products
- AGVs
- Full episode SHOW

## Training Parameters
- configuration file configuration.yaml 
- executable
- speed changed due to NavMesh
- Duplicates of the same system
- 4 environments running in parallel 
- Hardware used

## Usage
- Shop Floor Area
- Add Geofences
- Add Delivery Station
- Add Workstations
- Add Products
- Add AGV
- Fix Settings (Values and IDs)
- TRAIN (Training Mode) reference ML agents Toolkit (.exe creation and normal running)
- Test (use)
