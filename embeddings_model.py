import logging
import os.path
from typing import Any

import pandas as pd
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import accuracy_score, confusion_matrix, classification_report, matthews_corrcoef, \
    balanced_accuracy_score
from sklearn.model_selection import train_test_split
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
        self.parameters = 0

    def __str__(self) -> str:
        """
        Convert to a string.
        :return: The name.
        """
        return self.name

    def sentence_vectors(self, corpus: list[list[str]]) -> list:
        """
        Generate sentence vectors from the corpus.
        :param corpus: The corpus.
        :return: The sentence vectors.
        """
        embeddings = []
        for _ in corpus:
            embeddings.append(0)
        return embeddings

    def classify(self, corpus: list[list[str]], labels: list[str], output: str = "Embeddings",
                 seed: int = 42, classifier: Any or None = None) -> [float, float, float]:
        """
        Perform classification on the model.
        :param corpus: The corpus.
        :param labels: The labels of the corpus.
        :param output: Where to save the results to.
        :param seed: The random seed.
        :param classifier: The classifier to use.
        :return: The classification accuracy, balanced accuracy, and Matthews correlation coefficient.
        """
        # Get the sentence embeddings.
        logging.info(f"{self.name} | Getting sentence embeddings for the corpus...")
        embeddings = self.sentence_vectors(corpus)
        # Covert labels to integer values.
        logging.info(f"{self.name} | Converting labels...")
        label_encoder = LabelEncoder()
        encoded_labels = label_encoder.fit_transform(labels)
        # Do a train-test split.
        x_train, x_test, y_train, y_test = train_test_split(embeddings, encoded_labels, test_size=0.2,
                                                            random_state=seed)
        # Run classification.
        logging.info(f"{self.name} | Running classification...")
        # If no classifier was passed, this is not for Naive Bayes.
        if classifier is None:
            classifier = LogisticRegression(solver="lbfgs", random_state=seed)
        # Otherwise it is, so ensure the values are set.
        else:
            vectorizer = TfidfVectorizer()
            x_train = vectorizer.fit_transform(x_train)
            x_test = vectorizer.transform(x_test)
        classifier.fit(x_train, y_train)
        # Evaluate the classification.
        pred = classifier.predict(x_test)
        accuracy = accuracy_score(y_test, pred)
        cf = confusion_matrix(y_test, pred)
        report = classification_report(y_test, pred, output_dict=True)
        mcc = matthews_corrcoef(y_test, pred)
        balanced = balanced_accuracy_score(y_test, pred)
        logging.info(f"{self.name} | Accuracy = {accuracy} | Balanced Accuracy = {balanced} | Matthews Correlation "
                     f"Coefficient = {mcc}")
        # Ensure the root folder exists.
        if not os.path.exists(output):
            os.mkdir(output)
        output = os.path.join(output, "Classification")
        if not os.path.exists(output):
            os.mkdir(output)
        with open(os.path.join(output, f"{self.name}.csv"), "w") as file:
            file.write((f"Metric,Score"
                        f"\nAccuracy,{accuracy}"
                        f"\nBalanced Accuracy,{balanced}"
                        f"\nMatthews Correlation Coefficient,{mcc}"))
        # Make names better.
        report_index = {"accuracy": "Accuracy", "macro avg": "Macro Average", "weighted avg": "Weighted Average"}
        report_columns = {"precision": "Precision", "recall": "Recall", "f1-score": "F1", "support": "Support"}
        confusion_entries = []
        n = len(label_encoder.classes_)
        for i in range(n):
            value = label_encoder.inverse_transform([i])[0]
            report_index[str(i)] = value
            confusion_entries.append(value)
        # Save the classification report.
        metric_output = os.path.join(output, "Classification Report")
        if not os.path.exists(metric_output):
            os.mkdir(metric_output)
        df = pd.DataFrame(report).transpose()
        df = df.rename(index=report_index, columns=report_columns)
        df.to_csv(os.path.join(metric_output, f"{self.name}.csv"), index=True)
        # Save the confusion matrix.
        metric_output = os.path.join(output, "Confusion Matrix")
        if not os.path.exists(metric_output):
            os.mkdir(metric_output)
        df = pd.DataFrame(cf, index=confusion_entries, columns=confusion_entries)
        df.to_csv(os.path.join(metric_output, f"{self.name}.csv"))
        return accuracy, balanced, mcc
