import logging
import os

import numpy as np
from gensim.models import Word2Vec

from embeddings_model import EmbeddingsModel


# Configure logging.
logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")


class Word2VecModel(EmbeddingsModel):
    """
    word2vec model.
    """

    def __init__(self):
        """
        Initialize the word2vec model.
        """
        super().__init__("word2vec")

    def fit(self, corpus: list[list[str]], output: str = "Embeddings", seed: int = 42, params: dict = None) -> None:
        """
        Fit the model.
        :param corpus: The corpus to fit on.
        :param output: The output folder to save to.
        :param seed: The random seed.
        :param params: Any model parameters in a dictionary.
        :return: Nothing.
        """
        # Define standard parameters if none are passed.
        if params is None:
            params = {}
        alpha = params["Alpha"] if "Alpha" in params else 0.025
        window = params["Window"] if "Window" in params else 5
        negative = params["Negative"] if "Negative" in params else 5
        # If the model already exists, there is nothing to do.
        self.name = f"word2vec_alpha-{alpha}_window-{window}_negative-{negative}"
        path = os.path.join(output, f"{self.name}.txt")
        if os.path.exists(path):
            logging.info(f"Model '{path}' already exists.")
            return
        # Fit the model.
        logging.info(f"Fitting '{path}'...")
        self.model = Word2Vec(sentences=corpus, workers=os.cpu_count(), compute_loss=True, seed=seed, alpha=alpha,
                              window=window, negative=negative)
        # Save the model.
        if not os.path.exists(output):
            os.mkdir(output)
        self.model.save(f"{path}")
        # Save the loss.
        loss_output = f"{output}_Loss"
        if not os.path.exists(loss_output):
            os.mkdir(loss_output)
        with open(os.path.join(loss_output, f"{self.name}_loss.txt"), "w") as file:
            file.write(str(self.model.get_latest_training_loss()))
        logging.info(f"Fit '{path}.txt' | Loss = {self.model.get_latest_training_loss()}")

    def load(self, path: str) -> None:
        """
        Load the model from a path.
        :param path: The path to load the model from.
        :return: Nothing.
        """
        # Try to load the model.
        try:
            self.model = Word2Vec.load(path)
        # Revert if it fails.
        except Exception as e:
            logging.error(f"Error loading '{path}' as a Word2Vec model: {e}")
            self.model = None
            return
        # If successful, update the name.
        self.name = os.path.basename(path).split(".")[0]

    def sentence_vectors(self, corpus: list[list[str]]) -> list[float]:
        """
        Generate sentence vectors from the corpus.
        :param corpus: The corpus.
        :return: The sentence vectors.
        """
        if self.model is None:
            logging.error(f"Word2Vec model not initialized.")
            return super().sentence_vectors(corpus)
        embeddings = []
        for sentence in corpus:
            # Get only words in the model.
            words = [word for word in sentence if word in self.model.wv]
            # If there are words, get their embedding.
            if words:
                embeddings.append(np.mean(self.model.wv[words], axis=0))
            # Otherwise, it is simple zeros.
            else:
                embeddings.append(np.zeros(self.model.vector_size))
        return embeddings
