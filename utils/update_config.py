import os

import yaml

import wandb

# Initialize the wandb API
api = wandb.Api()

# Specify your project and group
project_name = 'diss'
group_name = 'eval_6x3x1_SPT'

config_path = None
# Define the configuration key-value pairs to add
config_updates = {
    'layout': '6x3x1',
    'method': 'SPT',
    'num_jobs': '6',
    'num_workstations': '3',
    'num_agvs': '1'
}

if config_path:
    # Load and update the config file
    with open(config_path, 'r') as f:
        config_updates.update(yaml.safe_load(f))

# Retrieve runs in the specified group
runs = api.runs(path=project_name, filters={"group": group_name})

# Iterate over each run in the group
for run in runs:
    # Reinitialize the run to update it
    with wandb.init(project=project_name, id=run.id, resume="allow", settings=wandb.Settings(_disable_stats=True)) as run:
        # Update the run's configuration with the new key-value pairs
        run.config.update(config_updates, allow_val_change=True)