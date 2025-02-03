import wandb
import yaml
import re
import os
import glob

def read_first_10_values(filename):
    values = []
    skip_first = True

    with open(filename, 'r') as file:
        for line in file:
            # Skip first value since it's biased
            if skip_first:
                skip_first = False
                continue

            parts = line.strip().split(',')
            if len(parts) == 3:
                values.append(float(parts[1] + '.' + parts[2]))
            elif len(parts) == 2:
                values.append(float(parts[1]))
            else:
                raise ValueError("Invalid file format.")
            if len(values) == 10:
                break

    if len(values) < 10:
        raise Exception("The file does not contain at least 10 unbiased values.")

    return values

def get_range_from_filename(filename):
    # Extract the range from the filename
    base = os.path.basename(filename)
    parts = base.split('-')
    range_part = parts[-2] + '-' + parts[-1].split('.')[0]
    return range_part

def get_seeds(range):
    parts = range.split('-')
    job_seed = int(parts[0])
    machine_seed = int(parts[1])
    return job_seed, machine_seed


def get_prefix_and_range_from_filename(filename):
    # Extract the prefix and range from the filename
    base = os.path.basename(filename)
    match = re.match(r"([a-zA-Z]+)-(\d+-\d+)\.csv", base)
    if match:
        prefix = match.group(1)
        range_part = match.group(2)
        return prefix, range_part
    else:
        raise ValueError(f"Invalid filename format: {filename}")

def pair_csv_files(path):
    # Get all CSV files in the directory
    csv_files = glob.glob(os.path.join(path, "*.csv"))

    # Separate job files from other files based on the prefix
    jobs_files = [file for file in csv_files if "Jobs" in os.path.basename(file)]
    other_files = [file for file in csv_files if "Jobs" not in os.path.basename(file)]

    # Create a dictionary to map ranges to job files
    jobs_dict = {get_range_from_filename(file): file for file in jobs_files}

    # Create a dictionary to map ranges to other files with prefixes
    other_files_dict = {}
    for file in other_files:
        prefix, range_part = get_prefix_and_range_from_filename(file)
        if range_part not in other_files_dict:
            other_files_dict[range_part] = {}
        other_files_dict[range_part][prefix] = file

    # Generate pairs
    paired_files = []
    for key in jobs_dict:
        if key in other_files_dict:
            for prefix, file in other_files_dict[key].items():
                paired_files.append((jobs_dict[key], file))

    return paired_files

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
root_path = r'/mnt/c/Users/Adrian/Downloads/resn/SPT'
file_pairs = pair_csv_files(root_path)


for job_file, result_file in file_pairs:
    results = read_first_10_values(result_file)
    job_filename = os.path.basename(job_file)
    result_filename = os.path.basename(result_file)
    seed_range = get_range_from_filename(job_filename)
    job_seed, machine_seed = get_seeds(seed_range)

    for i, result in enumerate(results):
        index = i+1
        # Paths to input and output files
        input_path = job_file
        output_path = job_file

        # Transform the data
        transform_data(input_path, output_path)
        layout='6x3x2'
        method='SPT'
        num_jobs=6
        num_workstations=3
        num_agvs=2

        # Initialize the run and ensure a new run is created
        run = wandb.init(project='diss_eval_fin',entity='iop-team', reinit=True, id=f'eval_{layout}_inst_{method}_{seed_range}_{index}', group=f'eval_{layout}_{method}', tags=[layout, 'eval', method], settings=wandb.Settings(_disable_stats=True))

        config_data = {}# Check if the configuration file exists
        config_path = os.path.join(root_path, 'configuration.yaml')
        if os.path.exists(config_path):
            # Load and update the config file
            with (
                open(config_path, 'r') as f):
                config_data = yaml.safe_load(f)
        else:
            print("Configuration file not found.")

        config_data['layout'] = layout
        config_data['method'] = method
        config_data['num_jobs'] = num_jobs
        config_data['num_workstations'] = num_workstations
        config_data['num_agvs'] = num_agvs
        config_data['job_seed'] = job_seed
        config_data['machine_seed'] = machine_seed

        wandb.config.update(config_data)

        # Path to your ONNX model
        onnx_model_path = os.path.join(root_path, 'AGV.onnx')

        # Check if the ONNX model file exists
        if os.path.exists(onnx_model_path):
            # Log the ONNX model file
            wandb.save(onnx_model_path)

            # Log the ONNX model as an artifact
            artifact = wandb.Artifact('onnx_model', type='model')
            artifact.add_file(onnx_model_path)
            run.log_artifact(artifact)
        else:
            print("ONNX model file not found.")

        # Log the transformed job data file as an artifact
        artifact = wandb.Artifact(job_filename, type='dataset')
        artifact.add_file(output_path)
        run.log_artifact(artifact)

        # Log a scalar value metric called "makespan"
        makespan = result

        # Log the makespan value
        wandb.log({'makespan': makespan})

        # Use wandb.summary to log the result
        wandb.run.summary['makespan'] = makespan

        # Finish the run
        run.finish()
