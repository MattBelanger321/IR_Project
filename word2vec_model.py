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

    def __init__(self, corpus: list[list[str]], output: str = "Embeddings", seed: int = 42, alpha: float = 0.025,
                 window: int = 5, negative: int = 5):
        """
        Initialize the word2vec model.
        :param corpus: The corpus to fit on.
        :param output: The output folder to save to.
        :param seed: The random seed.
        :param alpha: The alpha learning value.
        :param window: The word window.
        :param negative: The negative window.
        """
        super().__init__(f"word2vec Alpha={alpha} Window={window} Negative={negative}")
        # The "parameters" for sorting are given by larger hyperparameters.
        self.parameters = alpha + window + negative
        path = os.path.join(output, f"{self.name}.txt")
        # Fit the model.
        logging.info(f"Fitting '{path}'...")
        self.model = Word2Vec(sentences=corpus, workers=os.cpu_count(), compute_loss=True, seed=seed, alpha=alpha,
                              window=window, negative=negative)
        if not os.path.exists(output):
            os.mkdir(output)
        self.model.wv.save_word2vec_format(path, binary=False)
        # Save the loss.
        loss_output = os.path.join(output, "Loss")
        if not os.path.exists(loss_output):
            os.mkdir(loss_output)
        with open(os.path.join(loss_output, f"{self.name}.txt"), "w") as file:
            file.write(str(self.model.get_latest_training_loss()))
        logging.info(f"Fit '{path}.txt' | Loss = {self.model.get_latest_training_loss()}")

    def sentence_vectors(self, corpus: list[list[str]]) -> list:
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
