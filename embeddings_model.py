import logging
import os.path

from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import accuracy_score
from sklearn.preprocessing import LabelEncoder


# Configure logging.
logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")


class EmbeddingsModel:
    """
    Class for embeddings models to extend.
    """

    def __init__(self, name: str or None = None) -> None:
        """
        Initialize the embeddings model.
        :param name: The name of the embeddings model.
        """
        self.name = "embeddings_model" if name is None else name
        self.model = None

    def fit(self, corpus: list[list[str]], output: str = "Embeddings", seed: int = 42, params: dict = None) -> None:
        """
        Fit the model.
        :param corpus: The corpus to fit on.
        :param output: The output folder to save to.
        :param seed: The random seed.
        :param params: Any model parameters in a dictionary.
        :return: Nothing.
        """
        pass

    def load(self, path: str) -> None:
        """
        Load the model from a path.
        :param path: The path to load the model from.
        :return: Nothing.
        """
        pass

    def sentence_vectors(self, corpus: list[list[str]]) -> list[float]:
        """
        Generate sentence vectors from the corpus.
        :param corpus: The corpus.
        :return: The sentence vectors.
        """
        embeddings = []
        for _ in corpus:
            embeddings.append(0)
        return embeddings

    def classify(self, corpus: list[list[str]], labels: list[str], output: str = "Classifications",
                 seed: int = 42) -> float:
        """
        Perform classification on the model.
        :param corpus: The corpus.
        :param labels: The labels of the corpus.
        :param output: Where to save the accuracy result to.
        :param seed: The random seed.
        :return: The classification accuracy.
        """
        # Can't classify if there is no model.
        if self.model is None:
            logging.error(f"No model loaded.")
            return 0
        # Get the sentence embeddings.
        embeddings = self.sentence_vectors(corpus)
        # Covert labels to integer values.
        label_encoder = LabelEncoder()
        encoded_labels = label_encoder.fit_transform(labels)
        # Run classification.
        classifier = RandomForestClassifier(n_estimators=100, random_state=seed)
        classifier.fit(embeddings, encoded_labels)
        # Evaluate the classification.
        accuracy = accuracy_score(encoded_labels, classifier.predict(embeddings))
        if not os.path.exists(output):
            os.mkdir(output)
        with open(os.path.join(output, f"{self.name}.txt"), "w") as file:
            file.write(str(accuracy))
        return accuracy
