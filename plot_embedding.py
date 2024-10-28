import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import matplotlib
import os
import argparse
import logging
from sklearn.decomposition import PCA
from sklearn.manifold import TSNE
from sklearn.preprocessing import StandardScaler

matplotlib.use('TkAgg')  # Set the backend to a GUI-compatible option

# Set up logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

def load_data(file_path):
    """Load data from a file."""
    logging.info(f"Loading data from {file_path}...")
    
    if not os.path.exists(file_path):
        logging.error(f"File not found: {file_path}")
        return np.array([]), []
    
    lines = read_file(file_path)

    if not lines:
        logging.error(f"File is empty: {file_path}")
        return np.array([]), []
    
    return parse_lines(lines)

def read_file(file_path):
    """Read all lines from a file."""
    with open(file_path, 'r') as file:
        return file.readlines()

def parse_lines(lines):
    """Parse lines from the file into labels and data."""
    labels = []
    data = []
    
    for line in lines[1:]:  # Skip the first line for metadata
        parts = line.strip().split()
        if not is_valid_line(parts):
            logging.warning(f"Skipping invalid line: {line.strip()}")
            continue
        
        label, vector = extract_label_and_vector(parts)
        labels.append(label)
        data.append(vector)

    return convert_to_numpy(data, labels)

def is_valid_line(parts):
    """Check if the line has at least one label and one vector component."""
    return len(parts) >= 2

def extract_label_and_vector(parts):
    """Extract the label and vector from the parts."""
    label = parts[0]  # First element is the label
    vector = list(map(float, parts[1:]))  # Remaining elements are the vector
    return label, vector

def convert_to_numpy(data, labels):
    """Convert data list to a numpy array and log the result."""
    data_array = np.array(data)

    if data_array.size == 0:
        logging.error("No valid data points found in the file.")
        return data_array, labels

    logging.info(f"Loaded {len(data_array)} data points with {len(data_array[0])} dimensions.")
    return data_array, labels

def preprocess_data(data):
    """Standardize the data."""
    logging.info("Standardizing the data...")
    scaler = StandardScaler()
    scaled_data = scaler.fit_transform(data)
    logging.info("Data standardized.")
    return scaled_data

def perform_pca(data, n_components=2):
    """Perform PCA on the data."""
    logging.info("Performing PCA...")
    pca = PCA(n_components=n_components)
    reduced_data = pca.fit_transform(data)
    logging.info("PCA completed.")
    return reduced_data

def perform_tsne(data, n_components=2, perplexity=5):
    """Perform t-SNE on the data."""
    logging.info("Performing t-SNE...")
    tsne = TSNE(n_components=n_components, perplexity=perplexity)
    reduced_data = tsne.fit_transform(data)
    logging.info("t-SNE completed.")
    return reduced_data

def plot(reduced_data, labels, output_file, title):
    """Plot the reduced data with labels and save it to a PNG file."""
    logging.info(f"Plotting and saving the output to {output_file}...")
    plt.figure(figsize=(10, 10))
    scatter = plt.scatter(reduced_data[:, 0], reduced_data[:, 1], alpha=0.7)

    # Adding labels to points
    for i, label in enumerate(labels):
        plt.annotate(label, (reduced_data[i, 0], reduced_data[i, 1]), fontsize=8, alpha=0.6)

    plt.title(title)
    plt.xlabel('Component 1')
    plt.ylabel('Component 2')
    plt.grid()
    plt.savefig(f"{output_file}.png", format='png')
    logging.info(f"Plot saved as {output_file}.png")
    plt.close()

def get_file_path(category):
    """Get the file path for the embeddings based on the selected category."""
    if category:
        category_path = os.path.join(os.getcwd(), "embeddings", category)
        return os.path.join(category_path, "embeddings.txt")
    else:
        return os.path.join(os.getcwd(), "embeddings", "embeddings.txt")  # Path for big model

def main():
    # Set up argument parser
    parser = argparse.ArgumentParser(description="Perform PCA or t-SNE on a dataset.")
    parser.add_argument('--method', choices=['pca', 'tsne'], required=True, help="Analysis method: PCA or t-SNE.")
    parser.add_argument('--output', type=str, default='output', help="Output PNG file name (without extension).")
    parser.add_argument('--category', type=str, help="Select a category for analysis.")

    args = parser.parse_args()

    # Get file path based on the specified category
    file_path = get_file_path(args.category)
    data, labels = load_data(file_path)
    if data.size == 0:
        logging.error("No data to process. Exiting.")
        return

    numeric_data = preprocess_data(data)

    # Perform PCA or t-SNE based on the user's choice
    title = ""
    reduced_data = []
    if args.method == 'pca':
        reduced_data = perform_pca(numeric_data)
        title = 'PCA Analysis'
    elif args.method == 'tsne':
        reduced_data = perform_tsne(numeric_data)
        title = 't-SNE Analysis'

    plot(reduced_data, labels, args.output, title=title)

if __name__ == "__main__":
    main()
