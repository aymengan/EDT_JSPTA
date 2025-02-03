"""Minimal jobshop example."""
import collections
import os

from ortools.sat.python import cp_model
import re
import tempfile


"""Minimal jobshop problem."""
# Data.
jobs_data = [  # task =(machine_id, processing_time).
[(3,972),(1,170),(2,619)],
[(1,274),(2,748),(3,595)],
[(2,877),(1,283),(3,477)],
[(1,472),(3,898),(2,943)],
[(3,284),(2,807),(1,399)],
[(1,155),(3,695),(2,646)],
[(1,197),(3,951),(2,250)],
[(2,519),(1,836),(3,638)],
[(2,769),(3,683),(1,596)]
]


def convert_to_tuple_list(input_str):
    # Use regular expressions to find all tuples in the input string
    tuples = re.findall(r'\((\d+), ([\d.]+)\)', input_str)

    # Convert the found tuples from strings to a list of tuples with integers and floats
    tuple_list = [(int(x), float(y)) for x, y in tuples]

    return tuple_list


def convert_to_integer_tuple_list(tuple_list):
    converted_tuples = []
    max_decimal_places = 0

    for integer, float_val in tuple_list:
        # Convert float to string to count decimal places
        float_str = str(float_val)
        decimal_places = len(float_str.split('.')[1])

        # Update max_decimal_places if this float has more decimal places
        if decimal_places > max_decimal_places:
            max_decimal_places = decimal_places

        # Convert float to int by removing the comma and concatenating the decimal part
        converted_val = int(float_str.replace('.', ''))

        # Append the new tuple to the result list
        converted_tuples.append((integer, converted_val))

    return converted_tuples, max_decimal_places

def get_schedule_repr(schedule):
    output = ''
    all_machines = schedule.keys()

    for machine in all_machines:
        schedule[machine].sort(key=lambda x: x[1])
        sol_line_tasks = f'{machine}: '
        sol_line = '           '

        for assigned_task in schedule[machine]:
            name = assigned_task[0]
            sol_line_tasks += '%-15s' % name

            start = assigned_task[1]
            end = assigned_task[2]
            sol_tmp = '[%i,%i]' % (start, end)
            sol_line += '%-15s' % sol_tmp

        sol_line += '\n'
        sol_line_tasks += '\n'
        output += sol_line_tasks
        output += sol_line

    return output

def print_schedule(schedule):
    print(get_schedule_repr(schedule))

def get_solution():
    machines_count = 1 + max(task[0] for job in jobs_data for task in job)
    all_machines = range(machines_count)
    # Computes horizon dynamically as the sum of all durations.
    horizon = sum(task[1] for job in jobs_data for task in job)

    # Create the model.
    model = cp_model.CpModel()

    # Named tuple to store information about created variables.
    task_type = collections.namedtuple('task_type', 'start end interval')
    # Named tuple to manipulate solution information.
    assigned_task_type = collections.namedtuple('assigned_task_type',
                                                'start job index duration')

    # Creates job intervals and add to the corresponding machine lists.
    all_tasks = {}
    machine_to_intervals = collections.defaultdict(list)

    for job_id, job in enumerate(jobs_data):
        for task_id, task in enumerate(job):
            machine = task[0]
            duration = task[1]
            suffix = f'_{job_id}_{task_id}'
            start_var = model.NewIntVar(0, horizon, f'start{suffix}')
            end_var = model.NewIntVar(0, horizon, f'end{suffix}')
            interval_var = model.NewIntervalVar(start_var, duration, end_var, f'interval{suffix}')
            all_tasks[job_id, task_id] = task_type(start=start_var,
                                                   end=end_var,
                                                   interval=interval_var)
            machine_to_intervals[machine].append(interval_var)

    # Create and add disjunctive constraints.
    for machine in all_machines:
        model.AddNoOverlap(machine_to_intervals[machine])

    # Precedences inside a job.
    for job_id, job in enumerate(jobs_data):
        for task_id in range(len(job) - 1):
            model.Add(all_tasks[job_id, task_id +
                                1].start >= all_tasks[job_id, task_id].end)

    # Makespan objective.
    obj_var = model.NewIntVar(0, horizon, 'makespan')
    model.AddMaxEquality(obj_var, [
        all_tasks[job_id, len(job) - 1].end
        for job_id, job in enumerate(jobs_data)
    ])
    model.Minimize(obj_var)

    # Creates the solver and solve.
    solver = cp_model.CpSolver()
    status = solver.Solve(model)

    schedule = collections.defaultdict(list)
    if status == cp_model.OPTIMAL or status == cp_model.FEASIBLE:
        # Create one list of assigned tasks per machine.
        assigned_jobs = collections.defaultdict(list)
        for job_id, job in enumerate(jobs_data):
            for task_id, task in enumerate(job):
                machine = task[0]
                assigned_jobs[machine].append(
                    assigned_task_type(start=solver.Value(
                        all_tasks[job_id, task_id].start),
                                       job=job_id,
                                       index=task_id,
                                       duration=task[1]))

        for machine in all_machines:
            # Sort by starting time.
            assigned_jobs[machine].sort()
            sol_line_tasks = f'Machine {machine}: '
            sol_line = '           '

            for assigned_task in assigned_jobs[machine]:
                schedule[f'Machine {machine}'].append((f'job_{assigned_task.job}_task_{assigned_task.index}', assigned_task.start, assigned_task.start + assigned_task.duration))


        makespan = solver.ObjectiveValue()
        conflicts = solver.NumConflicts()
        branches = solver.NumBranches()
        wall_time = solver.WallTime()

        return makespan, schedule, conflicts, branches, wall_time
    else:
        raise Exception('No solution found.')


def perform_experiments():
    import wandb

    # Initialize the W&B API
    api = wandb.Api()

    # Define the project and list of groups
    project = 'diss'  # Replace with your project name
    entity = 'iop-team'  # Replace with your entity/username
    groups = ['group1', 'group2', 'group3']  # Replace with your list of groups

    # Fetch all runs in the project
    runs = api.runs(f"{entity}/{project}")

    # Filter runs by the specified groups
    filtered_runs = [run for run in runs if run.group in groups]

    # Create a set to track unique combinations of layout, job_seed, and machine_seed
    unique_combinations = set()
    representative_runs = []

    # Iterate over filtered runs to find unique combinations
    for run in filtered_runs:
        # Get the unique properties
        layout = run.config.get('layout')
        job_seed = run.config.get('job_seed')
        machine_seed = run.config.get('machine_seed')

        # Create a tuple of the unique properties
        combination = (layout, job_seed, machine_seed)

        # Check if this combination has been seen before
        if combination not in unique_combinations:
            unique_combinations.add(combination)
            representative_runs.append(run)

    # Process each representative run
    for rep_run in representative_runs:
        # Read some values
        layout = rep_run.config.get('layout')
        job_seed = rep_run.config.get('job_seed')
        machine_seed = rep_run.config.get('machine_seed')

        print(
            f"Processing run {rep_run.name} with layout: {layout}, job_seed: {job_seed}, machine_seed: {machine_seed}")

        dataset_artifact = None
        for artifact in rep_run.logged_artifacts():
            if artifact.type == 'dataset':
                dataset_artifact = artifact
                break

        # Check if the dataset artifact is found
        if dataset_artifact is None:
            raise ValueError("No dataset artifact found for this run")

        # Download the artifact to a temporary directory
        with tempfile.TemporaryDirectory() as tmpdirname:
            artifact_dir = dataset_artifact.download(tmpdirname)

            # Find the text file within the artifact
            text_file_path = None
            for root, dirs, files in os.walk(artifact_dir):
                for file in files:
                    if file.endswith(".csv"):
                        text_file_path = os.path.join(root, file)
                        break

            if text_file_path is None:
                raise ValueError("No text file found in the dataset artifact")

            # Read the contents of the text file using a temporary file
            with open(text_file_path, 'r') as file:
                job_repr = file.read()

        # Convert the input string to a list of tuples
        tuple_list = convert_to_tuple_list(job_repr)

        # Convert the list of tuples to a list of tuples with integers
        converted_tuples, max_decimal_places = convert_to_integer_tuple_list(tuple_list)





        # Do some stuff with the run
        # ...

        # Proceed to the next unique combination


if __name__ == '__main__':
    # get all runs, filter by layout, filter by job_seed, filter_by_machine_seed
    makespan, schedule, conflicts, branches, wall_time = get_solution()
    print(f"Optimal Schedule Length: {makespan}")
    print(f"schedule dict")
    print(schedule)
    print(f"Number of conflicts: {conflicts}")
    print(f"Number of branches: {branches}")
    print(f"Wall time: {wall_time}")
    print("prettyprint schedule")
    get_schedule_repr(schedule)
