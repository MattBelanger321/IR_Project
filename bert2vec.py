import os
import argparse
import logging
import torch
from transformers import BertTokenizer, BertModel
from tqdm import tqdm
import numpy as np

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
                with open(file_path, "r", encoding='utf-8') as file:
                    text = file.read()
                    corpus.append(text)
                    logging.info(f"Successfully loaded file: {file_path}")
            except Exception as e:
                logging.error(f"Error reading {file_path}: {e}")
    logging.info(f"Corpus loaded with {len(corpus)} documents.")
    return corpus

def get_bert_embeddings(texts, model, tokenizer, max_length=512):
    """
    Generate BERT embeddings for a list of texts.
    
    Args:
        texts (list): List of text documents
        model (BertModel): Pretrained BERT model
        tokenizer (BertTokenizer): BERT tokenizer
        max_length (int): Maximum sequence length
    
    Returns:
        numpy.ndarray: Average embeddings for each document
    """
    # Ensure model is in evaluation mode and on the correct device
    model.eval()
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    model.to(device)

    embeddings = []
    with torch.no_grad():
        for text in tqdm(texts, desc="Generating Embeddings"):
            # Tokenize and encode the text
            inputs = tokenizer(
                text, 
                return_tensors='pt', 
                max_length=max_length, 
                truncation=True, 
                padding='max_length'
            ).to(device)

            # Get model outputs
            outputs = model(**inputs)

            # Use the last hidden state, average across tokens (excluding padding)
            last_hidden_states = outputs.last_hidden_state
            
            # Create an attention mask to ignore padding tokens
            attention_mask = inputs['attention_mask']
            
            # Calculate mean embedding, ignoring padding tokens
            masked_embeddings = last_hidden_states * attention_mask.unsqueeze(-1)
            sum_embeddings = masked_embeddings.sum(dim=1)
            sum_mask = attention_mask.sum(dim=1).unsqueeze(-1)
            
            # Avoid division by zero
            sum_mask = torch.clamp(sum_mask, min=1e-9)
            
            # Calculate mean embedding
            mean_embedding = sum_embeddings / sum_mask
            
            # Move to CPU and convert to numpy
            embeddings.append(mean_embedding.cpu().numpy())

    return np.vstack(embeddings)

def save_embeddings(embeddings, output_path, words=None):
    """
    Save embeddings to a text file.
    
    Args:
        embeddings (numpy.ndarray): Embedding vectors
        output_path (str): Path to save embeddings
        words (list, optional): Optional list of corresponding words
    """
    logging.info("Saving embeddings...")
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    
    if words is None:
        # If no words provided, use document indices as identifiers
        words = [f"doc_{i}" for i in range(len(embeddings))]
    
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write(f"{len(embeddings)} {embeddings.shape[1]}\n")
        for word, embedding in zip(words, embeddings):
            embedding_str = ' '.join(map(str, embedding))
            f.write(f"{word} {embedding_str}\n")
    
    logging.info(f"Embeddings saved to: {output_path}")

def main():
    # Set up argument parser
    parser = argparse.ArgumentParser(description="Generate BERT embeddings for a corpus.")
    parser.add_argument("--model", default="bert-base-uncased", 
                        help="Pretrained BERT model to use")
    args = parser.parse_args()

    # Define paths
    root_dir = os.path.join(os.getcwd(), "arXiv_processed_mitigated")
    output_root_path = os.path.join(os.getcwd(), "bert_embeddings")

    # Create embeddings directory if it doesn't exist
    os.makedirs(output_root_path, exist_ok=True)

    # Load pretrained BERT model and tokenizer
    logging.info(f"Loading BERT model: {args.model}")
    tokenizer = BertTokenizer.from_pretrained(args.model)
    model = BertModel.from_pretrained(args.model)

    # Load entire corpus
    corpus = load_corpus(root_dir)

    if not corpus:
        logging.error("No documents found in the corpus. Exiting.")
        return

    # Generate embeddings for the entire corpus
    logging.info("Generating embeddings for entire corpus...")
    corpus_embeddings = get_bert_embeddings(corpus, model, tokenizer)
    
    # Save corpus-wide embeddings
    save_embeddings(corpus_embeddings, os.path.join(output_root_path, "bert_embeddings.txt"))

    logging.info("BERT embedding generation completed successfully.")

if __name__ == "__main__":
    main()