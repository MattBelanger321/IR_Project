import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import matplotlib
matplotlib.use('TkAgg')  # Set the backend to a GUI-compatible option
from sklearn.decomposition import PCA
from sklearn.manifold import TSNE
from sklearn.preprocessing import StandardScaler
import argparse
import logging

# Set up logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

def load_data(file_path):
    """Load data from a file."""
    logging.info(f"Loading data from {file_path}...")
    with open(file_path, 'r') as file:
        lines = file.readlines()
    
    # Read the first line for metadata
    metadata = lines[0].strip().split()
    
    # Initialize lists to hold labels and data
    labels = []
    data = []
    
    # Load the data, starting from the second line
    for line in lines[1:1000]:
        parts = line.strip().split()
        label = parts[0]  # First element is the label
        vector = list(map(float, parts[1:]))  # Remaining elements are the vector
        labels.append(label)
        data.append(vector)
    
    logging.info(f"Loaded {len(data)} data points with {len(data[0])} dimensions.")
    return np.array(data), labels

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

def perform_tsne(data, n_components=2, perplexity=30):
    """Perform t-SNE on the data."""
    logging.info("Performing t-SNE...")
    tsne = TSNE(n_components=n_components, perplexity=perplexity)
    reduced_data = tsne.fit_transform(data)
    logging.info("t-SNE completed.")
    return reduced_data

def plot(reduced_data, labels, output_file, title):
    """Plot the reduced data with labels and save it to a PNG file."""
    logging.info(f"Plotting and saving the output to {output_file}...")
    plt.figure(figsize=(12, 24))
    scatter = plt.scatter(reduced_data[:, 0], reduced_data[:, 1], alpha=0.7)

    # Adding labels to points
    for i, label in enumerate(labels):
        plt.annotate(label, (reduced_data[i, 0], reduced_data[i, 1]), fontsize=8, alpha=0.6)

    plt.title(title)
    plt.xlabel('Component 1')
    plt.ylabel('Component 2')
    plt.grid()
    plt.savefig(output_file)
    plt.show()
    logging.info(f"Interactive plot saved as {output_file}.png")
    plt.close()
    logging.info("Plot saved.")

def main():
    # Set up argument parser
    parser = argparse.ArgumentParser(description="Perform PCA or t-SNE on a dataset.")
    parser.add_argument('file_path', type=str, help="Path to the input file.")
    parser.add_argument('--method', choices=['pca', 'tsne'], required=True, help="Analysis method: PCA or t-SNE.")
    parser.add_argument('--output', type=str, default='output.png', help="Output PNG file name.")
    
    args = parser.parse_args()

    # Load and preprocess data
    data, labels = load_data(args.file_path)
    numeric_data = preprocess_data(data)

    # Perform PCA or t-SNE based on the user's choice
    title = ""
    reduced_data = []
    if args.method == 'pca':
        reduced_data = perform_pca(numeric_data)
        title='PCA Analysis'
    elif args.method == 'tsne':
        reduced_data = perform_tsne(numeric_data)
        title='t-SNE Analysis'
    plot(reduced_data, labels, args.output, title=title)

if __name__ == "__main__":
    main()
