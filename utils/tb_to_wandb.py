import wandb
import yaml
import re
import onnx
import os


# Function to transform the input data
def transform_data(input_path, output_path):
    with open(input_path, 'r') as file:
        lines = file.readlines()

    transformed_lines = []
    for line in lines:
        # Use regex to split while preserving the number parts
        parts = re.findall(r'(\w+|\d+,\d+|\d+)', line.strip())
        job_id = parts[0]
        operations = []
        for i in range(1, len(parts), 3):
            operation = int(parts[i])
            time_str = parts[i + 1] + '.' + parts[i + 2]
            time = float(time_str.replace(',', ''))
            operations.append(f'({operation}, {time:.6f})')
        transformed_line = f'{job_id}: {", ".join(operations)}'
        transformed_lines.append(transformed_line)

    with open(output_path, 'w') as file:
        file.write('\n'.join(transformed_lines))


# Root path to prepend to all file paths
root_path = r'/mnt/c/Users/Adrian/Desktop/diss-iop-v2/training results rand seed part broken cp/6x3x2_tm_2l_us'

# Initialize the run and resume it
run = wandb.init(project='diss_rand_seed_part_brok_train', resume='tm_2l_no_sc', entity='iop-team',
                 settings=wandb.Settings(_disable_stats=True))

# Load and update the config file
config_path = os.path.join(root_path, 'configuration.yaml')
with open(config_path, 'r') as f:
    config_data = yaml.safe_load(f)
wandb.config.update(config_data)

# Path to your ONNX model
onnx_model_path = os.path.join(root_path, 'AGV.onnx')

# Log the ONNX model file
wandb.save(onnx_model_path)

# Log the ONNX model as an artifact
artifact = wandb.Artifact('onnx_model', type='model')
artifact.add_file(onnx_model_path)
run.log_artifact(artifact)

# Optionally, you can load the ONNX model for use
model = onnx.load(onnx_model_path)

# Process all CSV files in the folder
for filename in os.listdir(root_path):
    if filename.endswith('.csv'):
        input_path = os.path.join(root_path, filename)
        output_filename = filename.replace('.csv', '.txt')
        output_path = os.path.join(root_path, output_filename)

        # Transform the data
        transform_data(input_path, output_path)

        # Log the transformed job data file as an artifact
        artifact = wandb.Artifact(f'job_data_{output_filename}', type='dataset')
        artifact.add_file(output_path)
        run.log_artifact(artifact)


# Check if the UsedSeeds folder exists and log all text files in it
used_seeds_path = os.path.join(root_path, 'UsedSeeds')
if os.path.exists(used_seeds_path) and os.path.isdir(used_seeds_path):
    for seed_filename in os.listdir(used_seeds_path):
        if seed_filename.endswith('.txt'):
            seed_file_path = os.path.join(used_seeds_path, seed_filename)
            artifact = wandb.Artifact(f'seeds_{seed_filename}', type='dataset')
            artifact.add_file(seed_file_path)
            run.log_artifact(artifact)

# Finish the wandb run
run.finish()
