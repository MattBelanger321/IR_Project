import argparse
import logging
import os
from typing import Any

import matplotlib
import matplotlib.pyplot as plt
import numpy as np
from sklearn.decomposition import PCA
from sklearn.manifold import TSNE
from sklearn.preprocessing import StandardScaler

# Set the backend to a GUI-compatible option.
matplotlib.use("TkAgg")

# Set up logging.
logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")


def save_plot(data: Any, labels: Any, output: str, title: str, do_labels: bool = True, size: float = 100) -> None:
    """
    Save a plot to a PNG.
    :param data: The data to plot.
    :param labels: The corresponding labels.
    :param output: The file to save to.
    :param title: The title of the plot.
    :param do_labels: If labels should be added.
    :param size: The size of the plots.
    :return: Nothing.
    """
    if size < 0:
        size = -size
    elif size == 0:
        size = 1
    plt.figure(figsize=(size, size))
    plt.scatter(data[:, 0], data[:, 1], alpha=0.7)
    # Adding labels to points.
    if do_labels:
        for i, label in enumerate(labels):
            plt.annotate(label, (data[i, 0], data[i, 1]), fontsize=8, alpha=0.6)
    # Add titles to the plot.
    plt.title(title)
    plt.xlabel("Component 1")
    plt.ylabel("Component 2")
    plt.grid()
    path = os.path.join(output, f"{title}.png")
    plt.savefig(path)
    logging.info(f"Saved plot '{path}'.")
    # Close the plot from memory after it is saved.
    plt.close()


def plot_embeddings(embeddings: str = "embeddings.txt", output: str = "Plots", perplexity: int = 20,
                    do_labels: bool = True, size: float = 100) -> None:
    """
    Perform PCA and t-SNE.
    :param embeddings: The embeddings file to use.
    :param output: The folder to output plots to.
    :param perplexity: Perplexity for t-SNE.
    :param do_labels: If labels should be added.
    :param size: The size of the plots.
    :return: Nothing.
    """
    # Nothing to do if the embeddings file does not exist.
    if not os.path.exists(embeddings):
        logging.error(f"'{embeddings}' does not exist.")
        return
    # Load the data.
    logging.info(f"Loading data from {embeddings}...")
    with open(embeddings, "r") as file:
        lines = file.readlines()
    labels = []
    data = []
    # Skip the first line for metadata.
    for line in lines[1:]:
        parts = line.strip().split()
        # Check if the line has at least one label and one vector component.
        if len(parts) < 2:
            logging.warning(f"Skipping invalid line: {line.strip()}")
            continue
        # First element is the label.
        labels.append(parts[0])
        # Remaining elements are the vector.
        data.append(list(map(float, parts[1:])))
    # Covert to a numpy array.
    data = np.array(data)
    # Nothing to do if there were no valid entries.
    if data.size == 0:
        logging.error(f"'{embeddings}' does not have any data.")
        return
    logging.info(f"Loaded {len(data)} data points with {len(data[0])} dimensions.")
    # Ensure the data is standardized.
    logging.info("Standardizing the data...")
    data = StandardScaler().fit_transform(data)
    # Create the output folder.
    if not os.path.exists(output):
        os.mkdir(output)
    # Perform PCA.
    logging.info("Performing PCA...")
    reduced = PCA(n_components=2).fit_transform(data)
    logging.info("Plotting PCA...")
    save_plot(reduced, labels, output, "PCA", do_labels, size)
    # Perform t-SNE.
    perplexity = max(1, perplexity)
    logging.info(f"Performing t-SNE with perplexity={perplexity}...")
    reduced = TSNE(n_components=2, perplexity=perplexity).fit_transform(data)
    logging.info("Plotting t-SNE...")
    save_plot(reduced, labels, output, f"t-SNE with perplexity of {perplexity}", do_labels, size)


if __name__ == "__main__":
    # Parse arguments.
    parser = argparse.ArgumentParser(description="Perform PCA and t-SNE")
    parser.add_argument("-e", "--embeddings", type=str, default="embeddings.txt", help="The embeddings file to use.")
    parser.add_argument("-o", "--output", type=str, default="Plots", help="The folder to output plots to.")
    parser.add_argument("-p", "--perplexity", type=int, default=20, help="Perplexity for t-SNE.")
    parser.add_argument("-s", "--size", type=float, default=100, help="The size of the plots.")
    parser.add_argument("-n", "--no-labels", action="store_false", help="Disable labels.")
    args = parser.parse_args()
    plot_embeddings(args.embeddings, args.output, args.perplexity, args.no_labels, args.size)
