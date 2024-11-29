import logging

import torch
from tqdm import tqdm

from embeddings_model import EmbeddingsModel

# Configure logging.
logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")


class TransformerModel(EmbeddingsModel):
    def __init__(self, max_length: int = 512) -> None:
        """
        Initialize the transformer model.
        :param max_length: The max input length.
        """
        super().__init__("transformer")
        self.tokenizer = None
        self.max_length = max_length

    def sentence_vectors(self, corpus: list[list[str]]) -> list:
        """
        Generate sentence vectors from the corpus.
        :param corpus: The corpus.
        :return: The sentence vectors.
        """
        embeddings = []
        self.model.eval()
        device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        self.model.to(device)
        texts = []
        for sentence in corpus:
            texts.append(" ".join(sentence))
        with torch.no_grad():
            for text in tqdm(texts, desc="Generating Embeddings"):
                # Tokenize and encode the text.
                inputs = self.tokenizer(text, return_tensors="pt", max_length=self.max_length, truncation=True,
                                        padding="max_length").to(device)
                outputs = self.model(**inputs)
                last_hidden_state = outputs.last_hidden_state
                attention_mask = inputs["attention_mask"]
                mask_expanded = attention_mask.unsqueeze(-1).expand(last_hidden_state.size()).float()
                sum_embeddings = torch.sum(last_hidden_state * mask_expanded, 1)
                sum_mask = torch.clamp(mask_expanded.sum(1), min=1e-9)
                embeddings.append((sum_embeddings / sum_mask).cpu()[0])
        return embeddings
