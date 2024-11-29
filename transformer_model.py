import logging

import torch
from tqdm import tqdm

from embeddings_model import EmbeddingsModel

# Configure logging.
logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")


class TransformerModel(EmbeddingsModel):
    """
    Transformer-based model.
    """

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
        # Ensure the model is in evaluation mode.
        self.model.eval()
        # Ensure we run on the GPU if available.
        device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        self.model.to(device)
        # The inputs need to be concatenated back into strings.
        with torch.no_grad():
            # Run for every entry in the corpus.
            for sentence in tqdm(corpus, desc="Generating Embeddings"):
                # Tokenize and encode the text.
                inputs = self.tokenizer(" ".join(sentence), return_tensors="pt", max_length=self.max_length,
                                        truncation=True, padding="max_length").to(device)
                last_hidden_state = self.model(**inputs).last_hidden_state
                mask_expanded = inputs["attention_mask"].unsqueeze(-1).expand(last_hidden_state.size()).float()
                embeddings.append((torch.sum(last_hidden_state * mask_expanded, 1) / torch.clamp(mask_expanded.sum(1),
                                                                                                 min=1e-9)).cpu()[0])
        return embeddings
