import argparse
import logging
import os.path
import random
import shutil

# Configure logging.
logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")


def create_training_set(directory: str, size: float or int = 0.1, seed: int = 42) -> None:
    """
    Create a training set.
    :param directory: The root folder to build a training set from.
    :param size: The amount to use for a training set, either as a percentage as a float in the range (0, 1) or the
    number of files to copy as an integer.
    :param seed: The seed for randomly choosing files.
    :return: Nothing.
    """
    # Ensure the folder exists.
    if not os.path.exists(directory):
        logging.error(f"Path '{directory}' does not exist.")
    # Nothing to do if the training folder already exists.
    training = f"{directory}_training"
    if os.path.exists(training):
        logging.error(f"Output directory '{training}' already exists.")
        return
    # Set the seed.
    random.seed = seed
    logging.info(f"Reading original files from directory '{directory}'...")
    # Load all potential files.
    corpus = []
    for folder, _, filenames in os.walk(directory):
        for filename in filenames:
            corpus.append(os.path.join(folder, filename))
    # Ensure valid size values.
    if isinstance(size, float):
        if size <= 0 or size >= 1:
            logging.error(f"The float value passed for 'size', {size}, must be in the range (0, 1).")
            return
        size = len(corpus) * size
    elif isinstance(size, int):
        total = len(corpus)
        if size <= 0 or size >= total:
            logging.error(f"The integer value passed for 'size', {size}, must be in the range (0, {total}).")
            return
    else:
        logging.error(f"'size' must be a float or integer.")
        return
    # Choose random samples.
    corpus = random.sample(corpus, size)
    # Copy files to the new training set.
    os.mkdir(training)
    copied = 1
    total = len(corpus)
    for entry in corpus:
        shutil.copy(entry, training)
        logging.info(f'Copied file {copied} of {total} - {os.path.join(training, os.path.basename(entry))}')
        copied += 1


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Training Set Creator")
    parser.add_argument("directory", type=str, help="The root folder to build a training set from.")
    parser.add_argument("--size", type=int or float, default=0.1, help="The amount to use for a training "
                                                                       "set, either as a percentage as a float in the "
                                                                       "range (0, 1) or the number of files to copy as "
                                                                       "an integer.")
    parser.add_argument("--seed", type=int, default=42, help="The seed for randomly choosing files.")
    args = parser.parse_args()
    create_training_set(args.directory, args.size, args.seed)
