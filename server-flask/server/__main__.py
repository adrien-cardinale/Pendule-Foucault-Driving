from flask import Flask, render_template, Response, jsonify
from camera import Camera
from plot import Plot
import json
import random
# from database import Database

app = Flask(__name__)

navs = [
    {'name': 'Home', 'url': '/'},
    {'name': 'Plot', 'url': '/plot'}
]

@app.context_processor
def inject_user():
    return dict(navs=navs)

@app.route('/')
def index():
    return render_template('index.html')

@app.route('/plot')
def plot():
    return render_template('plot.html')

def genCam(camera):
    while True:
        frame = camera.get_frame()
        yield (b'--frame\r\n'
               b'Content-Type: image/jpeg\r\n\r\n' + frame + b'\r\n')
  
@app.route('/camera_axis')
def video_feed():
    return Response(genPlot(Camera()),
                    mimetype='multipart/x-mixed-replace; boundary=frame')

@app.route('/donnees')
def get_donnees():
    data = []
    for _ in range(5):
        x = random.randint(0, 100)
        y = random.randint(0, 100)
        data.append({"x": x, "y": y})
    return jsonify(data)


if __name__ == '__main__':
    app.run(debug=True)