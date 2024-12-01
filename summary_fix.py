import os


def fix(directory: str = "arXiv_summaries") -> None:
    """
    Apply any manual fixes to existing summaries that we notice have text we don't want.
    :param directory: The directory of the summaries.
    :return: Nothing.
    """
    # Try every nested file.
    for folder, _, filenames in os.walk(directory):
        for filename in filenames:
            path = os.path.join(folder, filename)
            # Read the file.
            file = open(path, "r", errors="ignore")
            content = file.read()
            file.close()
            # Write the corrected version of the file.
            file = open(path, "w")
            file.write(content.replace("Here is a summary of the article in one sentence: ", ""))
            file.close()
            print(f"Updated file '{path}'")
