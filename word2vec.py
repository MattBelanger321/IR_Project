## This application will make a vector embedding of our arXiv corpus

import os

from gensim.models import Word2Vec
from gensim.utils import simple_preprocess


# Load the corpus.
corpus = []
root = os.path.join(os.getcwd(), "arXiv_processed")
for f in os.listdir(root):
    with open(os.path.join(root, f), "r") as file:
        corpus.append(simple_preprocess(file.read()))
# Fit the model, using all threads if possible.
model = Word2Vec(sentences=corpus, workers=os.cpu_count(), compute_loss=True)
# Save the model.
model.wv.save_word2vec_format(os.path.join(os.getcwd(), "embeddings.txt"), binary=False)
# Print the loss
print(model.get_latest_training_loss())
