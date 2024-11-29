from transformers import AutoModel, AutoTokenizer

from transformer_model import TransformerModel


class AutoTransformerModel(TransformerModel):
    def __init__(self, root: str, name: str, max_length: int = 512):
        """
        Initialize the E5 model.
        :param max_length: The max input length.
        """
        super().__init__(max_length)
        self.name = name
        self.root = root
        temp = f"{self.root}/{self.name}"
        self.model = AutoModel.from_pretrained(temp)
        self.tokenizer = AutoTokenizer.from_pretrained(temp)
        self.max_length = max_length
        self.embeddings = None

    def load(self, path: str) -> None:
        """
        Reset the model.
        :param path: The unused path.
        :return: Nothing.
        """
        temp = f"{self.root}/{self.name}"
        self.model = AutoModel.from_pretrained(temp)
        self.tokenizer = AutoTokenizer.from_pretrained(temp)
        self.embeddings = None
