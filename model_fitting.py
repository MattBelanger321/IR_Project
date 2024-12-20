import argparse
import logging
import os

from data_loader import load_data
from e5_model import E5TransformerModel
from embeddings_model import EmbeddingsModel
from mini_model import MiniTransformerModel
from naive_bayes_model import NaiveBayesModel
from word2vec_model import Word2VecModel


# Configure logging.
logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")


def evaluate_model(model: EmbeddingsModel, corpus: list[list[str]], labels: list[str], output: str = "Embeddings",
                   seed: int = 42) -> {str: float}:
    """
    Get the evaluations for a model.
    :param model: The model being evaluated.
    :param corpus: The corpus.
    :param labels: The labels.
    :param output: Where to save the results to.
    :param seed: The random seed.
    :return: The evaluations for the model.
    """
    accuracy, balanced, mcc = model.classify(corpus, labels, output, seed)
    return {"Accuracy": accuracy, "Balanced": balanced, "MCC": mcc, "Parameters": model.parameters}


def fit_best(directory: str = "arXiv_processed_mitigated", seed: int = 42,
             alphas: list or float = 0.025, windows: list or int = 5, negatives: int or None = 5) -> None:
    """
    Determine the best fitting model.
    :param directory: The directory to load the corpus from.
    :param seed: The random seed.
    :param alphas: word2vec alpha values.
    :param windows: word2vec window values.
    :param negatives: word2vec negative values.
    :return: Nothing.
    """
    # Create the output directory.
    if not os.path.exists("Embeddings"):
        os.mkdir("Embeddings")
    output = os.path.join("Embeddings", os.path.basename(directory))
    # Load the corpus.
    corpus, labels = load_data(directory)
    # Store all results.
    results = {}
    # Run Naive Bayes.
    nb = NaiveBayesModel()
    results[nb.name] = evaluate_model(nb, corpus, labels, output, seed)
    # Run all-MiniLM-L6-v2.
    mini = MiniTransformerModel()
    results[mini.name] = evaluate_model(mini, corpus, labels, output, seed)
    # Run e5-small-v2.
    e5 = E5TransformerModel()
    results[e5.name] = evaluate_model(e5, corpus, labels, output, seed)
    # Run the standard word2vec.
    w2v_standard = Word2VecModel(corpus, output, seed)
    results[f"{w2v_standard.name} - Standard"] = evaluate_model(w2v_standard, corpus, labels, output, seed)
    # Ensure fine-tuned word2vec models parameters are valid.
    if not isinstance(alphas, list):
        alphas = [alphas]
    if not isinstance(windows, list):
        windows = [windows]
    if not isinstance(negatives, list):
        negatives = [negatives]
    # Try all possible combinations.
    for alpha in alphas:
        for window in windows:
            for negative in negatives:
                logging.info(f"word2vec | Alpha = {alpha} | Window = {window} | Negative = {negative}")
                w2v = Word2VecModel(corpus, output, seed, alpha, window, negative)
                # If this model was the same as the standard version, skip it.
                if w2v.name != w2v_standard.name:
                    results[w2v.name] = evaluate_model(w2v, corpus, labels, output, seed)
    # Sort the results by best performing.
    results = dict(sorted(
        results.items(),
        key=lambda item: (-item[1]["Accuracy"], -item[1]["Balanced"], -item[1]["MCC"], item[1]["Parameters"], item[0])
    ))
    # Write the results to a CSV.
    s = "Model,Accuracy,Balanced Accuracy,Matthews Correlation Coefficient"
    for result in results:
        s += f"\n{result},{results[result]['Accuracy']},{results[result]['Balanced']},{results[result]['MCC']}"
    with open(os.path.join(output, "Classification", f"_Classification.csv"), "w") as file:
        file.write(s)


if __name__ == "__main__":
    # Add parsing commands.
    parser = argparse.ArgumentParser(description="Model Fitting")
    parser.add_argument("-d", "--directory", type=str, default="arXiv_processed_mitigated",
                        help="The folder to load data from.")
    parser.add_argument("-s", "--seed", type=int, default=42, help="The seed for random state.")
    parser.add_argument("-al", "--alpha_low", type=float, default=0.01, help="The lower bound for word2vec alpha "
                                                                             "values.")
    parser.add_argument("-au", "--alpha_upper", type=float, default=0.05, help="The upper bound for word2vec alpha "
                                                                               "values.")
    parser.add_argument("-as", "--alpha_step", type=float, default=0.01, help="The step for word2vec alpha values.")
    parser.add_argument("-wl", "--window_low", type=int, default=5, help="The lower bound for word2vec window "
                                                                         "values.")
    parser.add_argument("-wu", "--window_upper", type=int, default=10, help="The upper bound for word2vec window "
                                                                            "values.")
    parser.add_argument("-ws", "--window_step", type=int, default=5, help="The step for word2vec window values.")
    parser.add_argument("-nl", "--negative_low", type=int, default=5, help="The lower bound for word2vec negative "
                                                                           "values.")
    parser.add_argument("-nu", "--negative_upper", type=int, default=10, help="The upper bound for word2vec negative "
                                                                              "values.")
    parser.add_argument("-ns", "--negative_step", type=int, default=5, help="The step for word2vec negative values.")
    args = parser.parse_args()
    # Get the alpha values.
    current_alpha = args.alpha_low
    generated_alphas = [current_alpha]
    step = args.alpha_step
    upper = args.alpha_upper
    if step > 0:
        current_alpha += step
        while current_alpha <= upper:
            generated_alphas.append(current_alpha)
            current_alpha += step
    # Get the window values.
    current_window = args.window_low
    generated_windows = [current_window]
    step = args.window_step
    upper = args.window_upper
    if step > 0:
        current_window += step
        while current_window <= upper:
            generated_windows.append(current_window)
            current_window += step
    # Get the negative values.
    current_negative = args.negative_low
    generated_negatives = [current_negative]
    step = args.negative_step
    upper = args.negative_upper
    if step > 0:
        current_negative += step
        while current_negative <= upper:
            generated_negatives.append(current_negative)
            current_negative += step
    # Fit models.
    fit_best(args.directory, args.seed, generated_alphas, generated_windows, generated_negatives)
