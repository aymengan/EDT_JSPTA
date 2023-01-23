# EDT_JSPTA
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
- NavMesh package. 
- [Taillard](https://doi.org/10.1016/0377-2217(93)90182-M) JSP Generation
- PDRs - SPT and LPT (explain)
- [Learning to Dispatch](https://doi.org/10.48550/arXiv.2010.12367) Adjustments were made

## EDT Components
- **simple components (image + behavior)**
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
