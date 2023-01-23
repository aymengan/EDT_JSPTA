# EDT_JSPTA
This repository contains an Experimental Digital Twin (EDT) of a Job Shop with Transportation Agents (JSPTA) present in flexible manufacturing systems. Multiple methods for solving the JSPTA were implemented. i.e the shortest processing time and the longest processing time Priority Sispatching Rules (PDR) as well as an adapted Reinforcement Learning (RL) approach from the current State of the Art (SotA). 
Main Target Criteria makespan
Intenrtion to be a JSPTA solution test and developer tool

## Tools and Methods Used
The EDT was created using the [Unity](https://unity.com/) simulator and the [Unity ML-Agents Toolkit](https://github.com/Unity-Technologies/ml-agents). The agents and PDRs used within the enviroment solved for the system's AGV task allocation. Furthermore, the AGV navigation was handled using Unity's NavMesh package. 
[Learning to Dispatch](https://doi.org/10.48550/arXiv.2010.12367)
[Taillard](https://doi.org/10.1016/0377-2217(93)90182-M) JSP Generation

## EDT Components
simple components (image + behavior)
Shop Floor
Geofences
Delivery Stations
Workstations 
Products
AGV

## Training Parameters
configuration files

## Usage
Shop Floor Area
Add Geofences
Add Delivery Station
Add Workstations
Add Products
Add AGV
Fix Settings (Values and IDs)
TRAIN (Training Mode) reference ML agents Toolkit (.exe creation and normal running)
Test (use)
