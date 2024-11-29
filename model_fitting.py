from data_loader import load_data
from e5_model import E5TransformerModel
from mini_model import MiniTransformerModel
from word2vec_model import Word2VecModel

# TODO - USE PROPER VALUES AND MAKE INTO PROPER METHOD
corpus, labels = load_data("tiny")
w2v = Word2VecModel()
w2v.fit(corpus)
w2v.classify(corpus, labels)
#MiniTransformerModel().classify(corpus, labels)
#E5TransformerModel().classify(corpus, labels)
