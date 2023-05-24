import cv2

class Camera(object):
    def __init__(self):
        self.video = cv2.VideoCapture('rtsp://root:pendule2023@pendule.einet.ad.eivd.ch/axis-media/media.amp')

    def __del__(self):
        self.video.release()

    def get_frame(self):
        ret, frame = self.video.read()
        if ret:
            ret, jpeg = cv2.imencode('.jpg', frame)
            return jpeg.tobytes()
        else:
            return None