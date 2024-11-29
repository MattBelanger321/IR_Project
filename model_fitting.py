from data_loader import load_data
from word2vec_model import Word2VecModel

corpus, labels = load_data("tiny")
w2v = Word2VecModel()
w2v.fit(corpus)
