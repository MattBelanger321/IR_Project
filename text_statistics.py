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


def main(directory: str = "arXiv_processed", output: str = "Text Statistics", x: int = 10, y: int = 5) -> None:
    """
    Run text statistics on all categories together and each one individually.
    :param directory: The root folder to run these statistics on.
    :param output: The output directory to save to.
    :param x: The width of figures.
    :param y: The height of figures.
    :return: Nothing.
    """
    # Ensure the directory exists.
    if not os.path.exists(directory):
        logging.error(f"Directory '{directory}' does not exist.")
        return
    # Create the output directory.
    if not os.path.exists(output):
        os.mkdir(output)
    output = os.path.join(output, directory)
    if not os.path.exists(output):
        os.mkdir(output)
    logging.info(f"Determining text statistics for '{directory}'...")
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
            # Check every word in the text, running preprocessing to normalize and remove accents.
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
    # Get the top occurring words and counts.
    zipf_words = sorted(zipf, key=zipf.get, reverse=True)
    zipf_counts = sorted(zipf.values(), reverse=True)
    # Save the words to a CSV file from highest to lowest frequency.
    s = "Term,Frequency"
    n = len(zipf_words)
    for i in range(n):
        s += f"\n{zipf_words[i]},{zipf_counts[i]}"
    path = os.path.join(output, f"Frequencies.csv")
    with open(path, "w") as file:
        file.write(s)
    logging.info(f"Frequencies saved to '{path}'.")
    # Plot Heaps law.
    path = os.path.join(output, f"Heaps Law.png")
    save_plot(heaps, f"Heaps' Law", "Number of Documents (log10)", "Number of Terms (log10)", path,
              x, y)
    logging.info(f"Heaps' Law plot saved to '{path}'.")
    # Plot Zipf's law.
    path = os.path.join(output, f"Zipfs Law.png")
    save_plot(zipf_counts, f"Zipf's Law", "Rank (log10)", "Collection Frequency (log10)", path, x, y)
    logging.info(f"Zipf's Law plot saved to '{path}'.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Text Statistics")
    parser.add_argument("-d", "--directory", type=str, default="arXiv_processed", help="The root folder to run these "
                                                                                       "statistics on.")
    parser.add_argument("-o", "--output", type=str, default="Text Statistics", help="The output directory to save to.")
    parser.add_argument("-x", "--width", type=float, default=10, help="The output width.")
    parser.add_argument("-y", "--height", type=float, default=5, help="The output height.")
    args = parser.parse_args()
    main(args.directory, args.output, args.width, args.height)
