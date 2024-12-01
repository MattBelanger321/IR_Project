from auto_transformer_model import AutoTransformerModel


class MiniTransformerModel(AutoTransformerModel):
    def __init__(self, max_length: int = 512) -> None:
        """
        Initialize the MiniLM model.
        :param max_length: The max input length.
        """
        super().__init__("sentence-transformers", "all-MiniLM-L6-v2", max_length)
        self.parameters = 22700000
