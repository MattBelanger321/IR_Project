import argparse
import logging
import math
import os.path
import random
import shutil

# Configure logging.
logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")


def create_training_set(directory: str = "arXiv", size: float or int = 0.1, seed: int = 42,
                        categories: list[str] or None = None) -> None:
    """
    Create a training set.
    :param directory: The root folder to build a training set from.
    :param size: The amount to use for a training set, either as a percentage as a float in the range (0, 1) or the
    number of files to copy as an integer.
    :param seed: The seed for randomly choosing files.
    :param categories: The categories that random files can be chosen from.
    :return: Nothing.
    """
    # Ensure the folder exists.
    if not os.path.exists(directory):
        logging.error(f"Path '{directory}' does not exist.")
        return
    # Nothing to do if the training folder already exists.
    training = f"{directory}_training"
    if os.path.exists(training):
        logging.error(f"Output directory '{training}' already exists.")
        return
    # If no categories were passed, use all computer science categories.
    if categories is None:
        categories = [
            "cs.AI", "cs.AR", "cs.CC", "cs.CE", "cs.CG", "cs.CL", "cs.CR", "cs.CV", "cs.CY", "cs.DB", "cs.DC", "cs.DL",
            "cs.DM", "cs.DS", "cs.ET", "cs.FL", "cs.GL", "cs.GR", "cs.GT", "cs.HC", "cs.IR", "cs.IT", "cs.LG", "cs.LO",
            "cs.MA", "cs.MM", "cs.MS", "cs.NA", "cs.NE", "cs.NI", "cs.OH", "cs.OS", "cs.PF", "cs.PL", "cs.RO", "cs.SC",
            "cs.SD", "cs.SE", "cs.SI", "cs.SY"
        ]
    # Set the seed.
    random.seed = seed
    logging.info(f"Reading original files from directory '{directory}'...")
    # Load all potential files.
    corpus = []
    for folder, _, filenames in os.walk(directory):
        if os.path.basename(folder) in categories:
            for filename in filenames:
                corpus.append(os.path.join(folder, filename))
    # Ensure valid size values.
    total = len(corpus)
    print(total)
    if isinstance(size, float):
        if size <= 0 or size > 1:
            logging.error(f"The float value passed for 'size', {size}, must be in the range (0, 1].")
            return
        size = math.ceil(len(corpus) * size)
    elif isinstance(size, int):
        if size <= 0 or size > total:
            logging.error(f"The integer value passed for 'size', {size}, must be in the range (0, {total}].")
            return
    else:
        logging.error(f"'size' must be a float or integer.")
        return
    # Choose random samples.
    training_corpus = random.sample(corpus, size)
    # Copy files to the new training set.
    os.mkdir(training)
    copied = 1
    training_total = len(training_corpus)
    for entry in training_corpus:
        # Compute the relative path of the current directory to the source root.
        file_path = os.path.dirname(entry)
        relative = os.path.relpath(file_path, directory)
        # Determine the corresponding directory in the destination.
        destination = os.path.join(training, relative)
        # Ensure the destination directory exists.
        os.makedirs(destination, exist_ok=True)
        # Define the full source and destination file paths.
        filename = os.path.basename(entry)
        source_file = os.path.join(file_path, filename)
        destination_file = os.path.join(destination, filename)
        # Copy the file.
        shutil.copy(source_file, destination_file)
        logging.info(f'Copied file {copied} of {training_total} from {total} files - {destination_file}')
        copied += 1


def create_all_versions(directory: str = "arXiv", size: float or int = 0.1, seed: int = 42) -> None:
    """
    Create a training set for all needed folders.
    :param directory: The root folder to build a training set from.
    :param size: The amount to use for a training set, either as a percentage as a float in the range (0, 1) or the
    number of files to copy as an integer.
    :param seed: The seed for randomly choosing files.
    :return: Nothing.
    """
    # Copy all different folder types the same way.
    for extended in ["", "_processed", "_mitigated", "_processed_mitigated", "_summaries"]:
        create_training_set(f"{directory}{extended}", size, seed)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Training Set Creator")
    parser.add_argument("-d", "--directory", type=str, default="arXiv", help=("The root folder to build a "
                                                                              "training set from."))
    parser.add_argument("-s", "--size", type=int or float, default=0.1, help="The amount to use for a training "
                                                                             "set, either as a percentage as a float in"
                                                                             " the range (0, 1] or the number of files "
                                                                             "to copy as an integer.")
    parser.add_argument("-r", "--seed", type=int, default=42, help="The seed for randomly choosing files.")
    args = parser.parse_args()
    create_all_versions(args.directory, args.size, args.seed)
