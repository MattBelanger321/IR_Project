from AutoTransformerModel import AutoTransformerModel


class E5TransformerModel(AutoTransformerModel):
    def __init__(self, max_length: int = 512) -> None:
        """
        Initialize the E5 model.
        :param max_length: The max input length.
        """
        super().__init__("intfloat", "e5-small-v2", max_length)
        self.parameters = 33400000
