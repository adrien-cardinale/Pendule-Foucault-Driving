from flask import Flask, render_template, Response, jsonify
from .camera import Camera
import json
import os


app = Flask(__name__)

navs = [
    {'name': 'Home', 'url': '/'},
    {'name': 'Plot', 'url': '/plot'}
]

def create_app(test_config=None):
    # create and configure the app
    app = Flask(__name__, instance_relative_config=True)
    app.config.from_mapping(
        SECRET_KEY='dev',
        DATABASE=os.path.join(app.instance_path, 'flaskr.sqlite'),
    )
    if test_config is None:
        # load the instance config, if it exists, when not testing
        app.config.from_pyfile('config.py', silent=True)
    else:
        # load the test config if passed in
        app.config.from_mapping(test_config)

    # ensure the instance folder exists
    try:
        os.makedirs(app.instance_path)
    except OSError:
        pass

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
        return Response(genCam(Camera()),
                        mimetype='multipart/x-mixed-replace; boundary=frame')

    @app.route('/donnees')
    def get_donnees():
        with open('../data/data.json', 'r') as file:
            data = file.read()
        print(data)
        return jsonify(data)

    return app