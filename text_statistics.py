import argparse
import logging
import os
from pathlib import Path

import unicodedata
from matplotlib import pyplot as plt


# Set up logging.
logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")


def preprocess(s: str) -> list[str]:
    """
    Remove all accents and normalize the text.
    :param s: The text to normalize.
    :return: The normalized text split on whitespaces.
    """
    # Normalize the text to "NFKD" form and filter out combining characters.
    s = unicodedata.normalize("NFKD", s.lower())
    return "".join([c for c in s if not unicodedata.combining(c)]).split()


def save_plot(data: list, title: str, x_label: str, y_label: str, path: str, x: int = 10, y: int = 5) -> None:
    """
    Save a plot as an image.
    :param data: The data to plot.
    :param title: The title of the plot.
    :param x_label: The X label.
    :param y_label: The Y label.
    :param path: The path to save the image to.
    :param x: The width of figures.
    :param y: The height of figures.
    :return: Nothing.
    """
    fig = plt.figure(figsize=(x, y))
    plt.plot(data, linestyle="-", color="b")
    # The scales should be logs.
    plt.xscale("log")
    plt.yscale("log")
    plt.title(title)
    plt.xlabel(x_label)
    plt.ylabel(y_label)
    plt.tight_layout()
    fig.savefig(path)
    plt.close(fig)


def perform(directory: str, output: str, x: int = 10, y: int = 5) -> None:
    """
    Calculate the Heaps' and Zipf's laws.
    :param directory: The folder to run these statistics on.
    :param output: The output directory to save to.
    :param x: The width of figures.
    :param y: The height of figures.
    :return: Nothing.
    """
    logging.info(f"Determining text statistics for: {directory}")
    # Define the structures to hold the data.
    zipf = {}
    heaps = []
    # Store how many words we have found.
    total = 0
    # Check every file.
    for filepath in Path(directory).rglob("*"):
        if not filepath.is_file():
            continue
        with filepath.open("r") as file:
            # Check every word in the text, running preprocessing to normalize and remove accents..
            for word in preprocess(file.read()):
                # If the word exists, increment the number of occurrences.
                if word in zipf:
                    zipf[word] += 1
                # Otherwise, add it as it is a new word and increment the number of new words.
                else:
                    zipf[word] = 1
                    total += 1
            # Keep track of how many heaps instances we have.
            heaps.append(total)
    # Get the name for the files we will be saving to.
    name = os.path.basename(directory)
    # Get the top occurring words and counts.
    zipf_words = sorted(zipf, key=zipf.get, reverse=True)
    zipf_counts = sorted(zipf.values(), reverse=True)
    # Save the words to a CSV file from highest to lowest frequency.
    s = "Term,Frequency"
    n = len(zipf_words)
    for i in range(n):
        s += f"\n{zipf_words[i]},{zipf_counts[i]}"
    with open(os.path.join(output, f"{name}.csv"), "w") as file:
        file.write(s)
    # Plot Heaps law.
    save_plot(heaps, f"{name} Heaps' Law", "Number of Documents (log10)", "Number of Terms (log10)",
              os.path.join(output, f"{name} Heaps.png"), x, y)
    # Plot Zipf's law.
    save_plot(zipf_counts, f"{name} Zipf's Law", "Rank (log10)", "Collection Frequency (log10)",
              os.path.join(output, f"{name} Zipf.png"), x, y)


def main(directory: str, output: str, x: int = 10, y: int = 5) -> None:
    """
    Run text statistics on all categories together and each one individually.
    :param directory: The root folder to run these statistics on.
    :param output: The output directory to save to.
    :param x: The width of figures.
    :param y: The height of figures.
    :return: Nothing.
    """
    # Ensure the directory exists.
    directory = os.path.join(os.getcwd(), directory)
    if not os.path.exists(directory):
        logging.error(f"Root directory '{directory}' does not exist.")
        return
    output = os.path.join(os.getcwd(), output)
    if not os.path.exists(output):
        os.mkdir(output)
    logging.info(f"Saving text statistics to: {output}")
    # Perform across all categories.
    perform(directory, output, x, y)
    # Perform on each category individually.
    for category in os.listdir(directory):
        category = os.path.join(directory, category)
        if os.path.isdir(category):
            perform(category, output, x, y)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Text Statistics")
    parser.add_argument("directory", type=str, help="The root folder to run these statistics on.")
    parser.add_argument("output", type=str, help="The output directory to save to.")
    args = parser.parse_args()
    main(args.directory, args.output)
