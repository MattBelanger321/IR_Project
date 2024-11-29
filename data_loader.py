import logging
import os

from gensim.utils import simple_preprocess

# Configure logging.
logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")


def load_data(directory: str = "arXiv_processed_mitigated") -> (list[list[str]], list[str]):
    """
    Load data.
    :param directory: The directory to load data from.
    :return: Two lists composing of the corpus and the corresponding primary class labels.
    """
    # Ensure the folder exists.
    if not os.path.exists(directory):
        logging.error(f"Path '{directory}' does not exist.")
        return [], []
    # Load the contents and the class labels.
    corpus = []
    labels = []
    for folder, _, filenames in os.walk(directory):
        for filename in filenames:
            path = os.path.join(folder, filename)
            try:
                with open(path, "r") as file:
                    processed_text = simple_preprocess(file.read())
                    corpus.append(processed_text)
                    # The folder it is directly nested in is the class.
                    labels.append(os.path.basename(folder))
                    logging.info(f"Successfully loaded file '{path}' | Words: {len(processed_text)}")
            except Exception as e:
                logging.error(f"Error reading {path}: {e}")
    return corpus, labels
