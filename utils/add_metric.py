import wandb

# Initialize the wandb API
api = wandb.Api()

# Specify your project and group
project_name = 'diss'
group_name = 'eval_6x3x1_RL'

# Retrieve runs in the specified group
runs = api.runs(path=project_name, filters={"group": group_name})

# Iterate over each run in the group
for run in runs:
    # Retrieve the makespan value from the run's summary
    makespan = run.summary.get('makespan')

    if makespan is not None:
        # Reinitialize the run to update it
        wandb.init(project=project_name, id=run.id, resume="allow", settings=wandb.Settings(_disable_stats=True))

        # Log the makespan value
        wandb.log({'makespan': makespan})

        # Finish the wandb run
        wandb.finish()
