from typing import Any

from sklearn.naive_bayes import MultinomialNB

from embeddings_model import EmbeddingsModel


class NaiveBayesModel(EmbeddingsModel):
    """
    Class for Naive Bayes modelling.
    """
    
    def __init__(self) -> None:
        """
        Initialize the embeddings model.
        """
        super().__init__("Naive Bayes")

    def sentence_vectors(self, corpus: list[list[str]]) -> list:
        """
        Generate sentence vectors from the corpus.
        :param corpus: The corpus.
        :return: The sentence vectors.
        """
        embeddings = []
        for sentence in corpus:
            embeddings.append(" ".join(sentence))
        return embeddings

    def classify(self, corpus: list[list[str]], labels: list[str], output: str = "Embeddings",
                 seed: int = 42, classifier: Any or None = None) -> [float, float, float]:
        """
        Perform classification on the model.
        :param corpus: The corpus.
        :param labels: The labels of the corpus.
        :param output: Where to save the results to.
        :param seed: The random seed.
        :param classifier: The classifier to use which is not needed for this overload.
        :return: The classification accuracy, balanced accuracy, and Matthews correlation coefficient.
        """
        return super().classify(corpus, labels, output, seed, MultinomialNB())
