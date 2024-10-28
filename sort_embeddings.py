import os

# this applicaiton will sort embeddings.txt for readabillity
# we could modify this application to take arguments to allow us to sort by different metrics 
# to help us comprehend out data

# Path to the embeddings file
embeddings_path = os.path.join(os.getcwd(), "embeddings.txt")

# Load the embeddings, sort them, and save the sorted results
with open(embeddings_path, "r") as file:
    # Read all lines except the first line (contains metadata)
    header = file.readline()
    embeddings = file.readlines()

# Sort embeddings alphabetically by word (each line starts with a word followed by its vector)
embeddings.sort(key=lambda line: line.split()[0])

# Write the header and sorted embeddings back to the file
with open(embeddings_path, "w") as file:
    file.write(header)
    file.writelines(embeddings)

print("Embeddings sorted alphabetically.")
