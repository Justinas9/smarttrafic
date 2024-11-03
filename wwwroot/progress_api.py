from flask import Flask, jsonify
import sys

app = Flask(__name__)

# Ensure you import your recognition script correctly
# Assuming get_progress() is available after running recognition.py
# Importing the function directly won't work, you need to run the script and then access the progress

# Endpoint to get progress
@app.route('/video/progress/<int:session_id>', methods=['GET'])
def video_progress(session_id):
    # Call the get_progress function from your processing script
    current_progress = get_progress()  # Ensure get_progress returns session-specific progress if necessary
    return jsonify({"progress": current_progress, "message": "Vaizdo įrašas apdorojamas..."})

if __name__ == "__main__":
    app.run(port=5000)  # Run on your desired port
