import os
import random

cs_max_int = 2147483647

def load_used_seeds(directory):
    used_seeds = set()

    for filename in os.listdir(directory):
        if filename.endswith(".txt"):
            filepath = os.path.join(directory, filename)
            with open(filepath, 'r') as file:
                for line in file:
                    try:
                        job_seed, machine_seed = map(int, line.strip().split(','))
                        used_seeds.add(job_seed)
                        used_seeds.add(machine_seed)
                    except ValueError:
                        print("Exception thrown")
                        print("We handled the following line:")
                        print(line)
                        continue

    return used_seeds


def sample_new_seeds(used_seeds, num_samples):
    new_seeds = []
    while len(new_seeds) < num_samples:
        job_seed = random.randint(0, cs_max_int)
        machine_seed = random.randint(0, cs_max_int)
        if job_seed not in used_seeds and machine_seed not in used_seeds:
            new_seeds.append((job_seed, machine_seed))
            used_seeds.add(job_seed)
            used_seeds.add(machine_seed)
    return new_seeds


def main(directory, num_samples):
    used_seeds = load_used_seeds(directory)
    new_seeds = sample_new_seeds(used_seeds, num_samples)

    # Output the new seeds
    return new_seeds


# Test the code using the uploaded files
test_directory = r"/mnt/c/Users/Adrian/Desktop/diss-iop-v2/train_res/all seeds"
num_samples = 5  # Sample 10 new seeds

sampled_seeds = main(test_directory, num_samples)
print(sampled_seeds)
