import os
import argparse
import logging
from gensim.models import Word2Vec
from gensim.utils import simple_preprocess

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

def load_corpus(root_dir):
    """Recursively loads and preprocesses text files from the given directory into a corpus list."""
    logging.info(f"Loading corpus from directory: {root_dir}")
    corpus = []
    for dirpath, _, filenames in os.walk(root_dir):
        for filename in filenames:
            file_path = os.path.join(dirpath, filename)
            try:
                with open(file_path, "r") as file:
                    processed_text = simple_preprocess(file.read())
                    corpus.append(processed_text)
                    logging.info(f"Successfully loaded file: {file_path} (Words: {len(processed_text)})")
            except Exception as e:
                logging.error(f"Error reading {file_path}: {e}")
    logging.info(f"Corpus loaded with {len(corpus)} documents.")
    return corpus

def fit_model(corpus, model_path):
    """Fits a Word2Vec model on the provided corpus and saves it to model_path."""
    logging.info("Starting model training...")
    model = Word2Vec(sentences=corpus, workers=os.cpu_count(), compute_loss=True)
    model.save(model_path)
    logging.info(f"Model trained and saved to: {model_path}")
    logging.info("Training loss: %f", model.get_latest_training_loss())
    return model

def load_model(model_path):
    """Loads a pre-trained Word2Vec model from the specified file."""
    logging.info(f"Loading model from: {model_path}")
    return Word2Vec.load(model_path)

def save_embeddings(model, output_path):
    """Saves the embeddings from the given Word2Vec model to the specified output path."""
    logging.info("Saving embeddings...")
    model.wv.save_word2vec_format(output_path, binary=False)
    logging.info(f"Embeddings saved to: {output_path}")

def check_corpus(corpus):
    """Checks if the corpus is empty and exits if so."""
    if not corpus:
        logging.warning("No documents found in the specified directory. Exiting.")
        return False
    logging.info(f"Corpus contains {len(corpus)} documents. Proceeding...")
    return True

def get_model(corpus, model_path, fit_model_flag):
    """Gets a Word2Vec model, fitting it if requested or loading it if it exists."""
    if fit_model_flag:
        logging.info("Fitting a new Word2Vec model...")
        return fit_model(corpus, model_path)
    else:
        if os.path.exists(model_path):
            logging.info("Loading existing model.")
            return load_model(model_path)
        else:
            logging.warning("No saved model found. Use --fit_model to train a new model.")
            return None

def process_category_model(category, fit_model_flag, output_root_path):
    """Processes a category to fit a Word2Vec model and save embeddings."""
    category_path = os.path.join(os.getcwd(), "arXiv_processed", category)
    logging.info(f"Processing category: {category}")

    # Load corpus for the category
    category_corpus = load_corpus(category_path)

    # Check if corpus is empty
    if not check_corpus(category_corpus):
        return

    category_model_path = os.path.join(output_root_path, category, "word2vec.model")
    os.makedirs(os.path.dirname(category_model_path), exist_ok=True)

    # Get the model for the category
    category_model = get_model(category_corpus, category_model_path, fit_model_flag)
    if category_model is not None:
        save_embeddings(category_model, os.path.join(output_root_path, category, "embeddings.txt"))

def main():
    # Set up argument parser
    parser = argparse.ArgumentParser(description="Fit a Word2Vec model on a corpus.")
    parser.add_argument("--fit_model", action="store_true", help="If set, fits the Word2Vec model.")
    args = parser.parse_args()

    # Define paths
    root_dir = os.path.join(os.getcwd(), "arXiv_processed")
    big_model_path = os.path.join(os.getcwd(), "word2vec_embeddings", "word2vec.model")
    output_root_path = os.path.join(os.getcwd(), "word2vec_embeddings")

    # Create embeddings directory if it doesn't exist
    os.makedirs(output_root_path, exist_ok=True)

    # Load corpus
    corpus = load_corpus(root_dir)

    # Check if corpus is empty
    if not check_corpus(corpus):
        return

    # Fit and save the big model
    big_model = get_model(corpus, big_model_path, args.fit_model)
    if big_model is None:
        return

    # Save the embeddings from the big model
    save_embeddings(big_model, os.path.join(output_root_path, "embeddings.txt"))

    # Process each category
    for category in os.listdir(root_dir):
        if os.path.isdir(os.path.join(root_dir, category)):
            process_category_model(category, args.fit_model, output_root_path)

    logging.info("Process completed successfully.")

if __name__ == "__main__":
    main()
