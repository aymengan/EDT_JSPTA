import pandas as pd
import wandb
import seaborn as sns
import matplotlib.pyplot as plt

# Initialize the Weights and Biases API
api = wandb.Api()

# Project is specified by <entity/project-name>
project_name = "iop-team/diss_eval_fin"
runs = api.runs(project_name)

# Lists to store information from each run
summary_list, config_list, name_list = [], [], []
all_keys = set()

# Loop through each run in the project
for run in runs:
    summary = run.summary._json_dict
    config = {k: v for k, v in run.config.items() if not k.startswith('_')}
    name = run.name

    summary_list.append(summary)
    config_list.append(config)
    name_list.append(name)

    # Collect all unique keys from summary and config
    all_keys.update(summary.keys())
    all_keys.update(config.keys())

# Create a dataframe with all collected keys as columns
all_data = []
for summary, config, name in zip(summary_list, config_list, name_list):
    data = {key: None for key in all_keys}
    data.update(summary)
    data.update(config)
    data["name"] = name
    all_data.append(data)

runs_df = pd.DataFrame(all_data)

# Create the plot
plt.figure(figsize=(12, 8))
plot = sns.barplot(data=runs_df, x="method", y="makespan")

# Rotate x-axis labels for better readability
plot.set_xticklabels(plot.get_xticklabels(), rotation=45, ha="right")

# Show the plot
plt.tight_layout()
plt.show()

plot.figure.savefig("training_comp_fin.png", dpi=300)