import cv2
import pyodbc
from datetime import datetime, timedelta
from ultralytics import YOLO
import uuid
import numpy as np
import sys

# Load the YOLOv8 model
model = YOLO('yolov8n.pt')

# Retrieve arguments from the command line
video_path = sys.argv[1]  # The path to the video file
start_time = datetime.fromisoformat(sys.argv[2])  # Start time for processing
coordinates_x = float(sys.argv[3])  # X coordinate
coordinates_y = float(sys.argv[4])  # Y coordinate
request_id = int(sys.argv[5])

# Video capture
cap = cv2.VideoCapture(video_path)

# Database connection setup
conn = pyodbc.connect(
    'DRIVER={ODBC Driver 17 for SQL Server};'
    'SERVER=PC\\SQLEXPRESS;'
    'DATABASE=master;'
    'Trusted_Connection=yes;'
)
cursor = conn.cursor()

# Function to batch insert detection data
def batch_insert_detections(detections):
    query = '''INSERT INTO ObjectDetections (ObjectID, Probability, DetectionTime, ObjectCount, RequestID, BatchNumber) 
                VALUES (?, ?, ?, ?, ?, ?)'''
    cursor.executemany(query, detections)
    conn.commit()

# ObjectTracker class definition
class ObjectTracker:
    def __init__(self, max_age=10, min_hits=3):
        self.next_id = 1
        self.tracked_objects = {}
        self.max_age = max_age  # Max frames an object can be undetected
        self.min_hits = min_hits  # Minimum frames to confirm an object
        self.object_counts = {'car': 0, 'motorcycle': 0, 'bus': 0, 'person': 0, 'bicycle': 0}
        self.confirmed_objects = set()

    def update(self, detections):
        updated_tracked_objects = {}
        new_detections = []

        # Process current detections
        for det in detections:
            cls, box, probability = det
            x1, y1, x2, y2 = map(int, box)
            centroid = ((x1 + x2) // 2, (y1 + y2) // 2)
            box_size = (x2 - x1) * (y2 - y1)

            # Try to match with existing tracked objects
            best_match = None
            best_distance = float('inf')
            for obj_id, obj_data in self.tracked_objects.items():
                prev_centroid = obj_data['centroid']
                prev_box_size = obj_data['box_size']

                # Calculate distance and size similarity
                distance = np.linalg.norm(np.array(centroid) - np.array(prev_centroid))
                size_similarity = abs(box_size - prev_box_size) / prev_box_size

                if distance < 75 and size_similarity < 0.4 and cls == obj_data['type']:
                    if distance < best_distance:
                        best_match = obj_id
                        best_distance = distance

            if best_match is not None:
                # Update existing object
                obj_data = self.tracked_objects[best_match]
                obj_data['centroid'] = centroid
                obj_data['box_size'] = box_size
                obj_data['frames_seen'] += 1
                obj_data['frames_unseen'] = 0
                updated_tracked_objects[best_match] = obj_data

                if best_match not in self.confirmed_objects and obj_data['frames_seen'] >= self.min_hits:
                    self.object_counts[cls] += 1
                    self.confirmed_objects.add(best_match)
                    new_detections.append((cls, probability, obj_data['frames_seen']))
            else:
                # New object detection
                new_id = self.next_id
                self.next_id += 1
                updated_tracked_objects[new_id] = {
                    'centroid': centroid,
                    'box_size': box_size,
                    'frames_seen': 1,
                    'frames_unseen': 0,
                    'type': cls
                }

        # Age out and remove old tracked objects
        self.tracked_objects = {
            obj_id: obj_data for obj_id, obj_data in updated_tracked_objects.items()
            if obj_data['frames_unseen'] < self.max_age
        }

        # Increment frames unseen for unmatched objects
        for obj_id in updated_tracked_objects:
            if obj_id not in self.tracked_objects:
                self.tracked_objects[obj_id]['frames_unseen'] += 1

        return new_detections

# Initialize tracker
tracker = ObjectTracker()

# Initialize frame count and batch ID
frame_count = 0
batch_id = 1  # Starting batch ID

# Store detections and set batch insertion interval
batch_detections = []
batch_interval = timedelta(seconds=5)
last_batch_time = datetime.now()

# Run detection for the entire video duration
while cap.isOpened():
    ret, frame = cap.read()
    if not ret:
        break

    current_pos_sec = cap.get(cv2.CAP_PROP_POS_MSEC) / 1000  # Current position in seconds
    detection_time = start_time + timedelta(seconds=current_pos_sec)

    # Process every 5th frame to reduce computation
    if frame_count % 5 == 0:
        # Perform object detection on the current frame
        results = model.predict(source=frame, classes=[2, 3, 5, 0, 1], conf=0.4)

        # Prepare detections for tracking
        current_detections = []
        for result in results[0].boxes:
            box = result.xyxy[0]
            cls = int(result.cls[0])
            probability = float(result.conf[0])

            # Map class index to object type
            obj_type = {2: 'car', 3: 'motorcycle', 5: 'bus', 0: 'person', 1: 'bicycle'}.get(cls)
            if obj_type:
                current_detections.append((obj_type, box, probability))

        # Update tracker and get new detections
        new_batch_detections = tracker.update(current_detections)

        # Prepare batch detections for database
        for cls, probability, frames_seen in new_batch_detections:
            batch_detections.append(( 
                cls,  # ObjectType
                probability,
                detection_time,
                tracker.object_counts[cls],
                request_id,  # Request ID
                batch_id  # Batch ID
            ))

        # Batch insert every 5 seconds
        if (datetime.now() - last_batch_time) >= batch_interval:
            if batch_detections:
                batch_insert_detections(batch_detections)
                batch_detections.clear()
                batch_id += 1  # Increment batch ID for the next batch
            last_batch_time = datetime.now()

        # Draw bounding boxes, object info, and object ID on the frame
        for obj_id, obj_data in tracker.tracked_objects.items():
            centroid = obj_data['centroid']
            cls = obj_data['type']
            count = tracker.object_counts[cls]

            # Draw the bounding box
            x1, y1 = centroid[0] - 25, centroid[1] - 25  # Adjust position slightly around the centroid
            x2, y2 = centroid[0] + 25, centroid[1] + 25
            cv2.rectangle(frame, (x1, y1), (x2, y2), (0, 255, 0), 2)

            # Display the object type, count, and unique ID
            cv2.putText(frame, f"{cls}, Count: {count}, ID: {obj_id}",
                        (x1, y1 - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 255), 2)

        # Display the frame
        frame_resized = cv2.resize(frame, (1280, 720))
        cv2.imshow('Video with Object Detection', frame_resized)

        # Move window to center of screen
        cv2.moveWindow('Video with Object Detection', 320, 180)

        # Release frame and results to free memory after each batch
        frame = None
        results = None

        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    frame_count += 1

# Final batch insert in case any data remains
if batch_detections:
    batch_insert_detections(batch_detections)

cap.release()
cv2.destroyAllWindows()
cursor.close()
conn.close()
