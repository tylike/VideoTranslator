window.timelineViewer = {
    getBoundingClientRect: (element) => {
        const rect = element.getBoundingClientRect();
        return {
            x: rect.x,
            y: rect.y,
            width: rect.width,
            height: rect.height,
            top: rect.top,
            left: rect.left,
            bottom: rect.bottom,
            right: rect.right
        };
    },

    seekVideo: (time, dotNetObject) => {
        const video = document.getElementById('videoPlayer');
        if (video) {
            console.log('=== Seek Video ===');
            console.log('Seeking video to time:', time);
            console.log('Video readyState:', video.readyState);
            console.log('Video duration:', video.duration);
            console.log('Video currentTime (before):', video.currentTime);
            
            if (video.readyState >= 1) {
                video.currentTime = time;
                console.log('Video currentTime (after):', video.currentTime);
                
                video.addEventListener('seeked', function onSeeked() {
                    video.removeEventListener('seeked', onSeeked);
                    console.log('Video seeked event fired, currentTime:', video.currentTime);
                    if (dotNetObject) {
                        dotNetObject.invokeMethodAsync('OnVideoSeeked', video.currentTime);
                    }
                }, { once: true });
                
                setTimeout(() => {
                    console.log('Video currentTime (after 100ms):', video.currentTime);
                }, 100);
            } else {
                console.log('Video not ready, waiting for metadata...');
                video.addEventListener('loadedmetadata', function onLoaded() {
                    video.removeEventListener('loadedmetadata', onLoaded);
                    video.currentTime = time;
                    console.log('Video currentTime (after load):', video.currentTime);
                    
                    video.addEventListener('seeked', function onSeeked() {
                        video.removeEventListener('seeked', onSeeked);
                        console.log('Video seeked event fired (after load), currentTime:', video.currentTime);
                        if (dotNetObject) {
                            dotNetObject.invokeMethodAsync('OnVideoSeeked', video.currentTime);
                        }
                    }, { once: true });
                }, { once: true });
            }
        } else {
            console.error('Video element not found');
        }
    },

    getClickPosition: (element, clientX, zoom) => {
        const rect = element.getBoundingClientRect();
        const scrollLeft = element.scrollLeft;
        const paddingLeft = parseFloat(getComputedStyle(element).paddingLeft);
        const viewportWidth = rect.width - paddingLeft - parseFloat(getComputedStyle(element).paddingRight);
        const clickX = clientX - rect.left - paddingLeft;
        const position = ((clickX + scrollLeft) / viewportWidth) * 100;
        return Math.max(0, Math.min(100, position));
    },

    getMouseMovePosition: (element, clientX, zoom) => {
        const rect = element.getBoundingClientRect();
        const scrollLeft = element.scrollLeft;
        const paddingLeft = parseFloat(getComputedStyle(element).paddingLeft);
        const viewportWidth = rect.width - paddingLeft - parseFloat(getComputedStyle(element).paddingRight);
        const mouseX = clientX - rect.left - paddingLeft;
        const position = ((mouseX + scrollLeft) / viewportWidth) * 100;
        return Math.max(0, Math.min(100, position));
    },

    initResize: (dotNetObject) => {
        const resizer = document.getElementById('resizer');
        const videoSection = document.getElementById('video-section');
        
        if (!resizer || !videoSection) {
            console.error('Elements not found:', { resizer, videoSection });
            return;
        }

        let isResizing = false;
        let startY = 0;
        let startHeight = 0;

        const onMouseDown = (e) => {
            isResizing = true;
            startY = e.clientY;
            startHeight = videoSection.offsetHeight;
            document.body.style.cursor = 'ns-resize';
            document.body.style.userSelect = 'none';
            e.preventDefault();
        };

        const onMouseMove = (e) => {
            if (!isResizing) return;
            
            const deltaY = e.clientY - startY;
            const newHeight = Math.max(200, Math.min(800, startHeight + deltaY));
            videoSection.style.height = newHeight + 'px';
            
            dotNetObject.invokeMethodAsync('OnResize', newHeight);
        };

        const onMouseUp = () => {
            if (isResizing) {
                isResizing = false;
                document.body.style.cursor = '';
                document.body.style.userSelect = '';
            }
        };

        resizer.addEventListener('mousedown', onMouseDown);
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);

        return () => {
            resizer.removeEventListener('mousedown', onMouseDown);
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
        };
    },
    
    initSubtitles: (dotNetObject) => {
        const video = document.getElementById('videoPlayer');
        if (!video) return;
        
        video.volume = 0.3;
        
        const textTracks = video.textTracks;
        console.log('字幕轨道数量:', textTracks.length);
        
        for (let i = 0; i < textTracks.length; i++) {
            const track = textTracks[i];
            console.log(`字幕轨道 ${i}:`, track.label, track.language, track.kind, track.mode);
            
            track.mode = 'showing';
            
            track.addEventListener('cuechange', () => {
                console.log(`cuechange 事件触发 - 轨道 ${i}, activeCues: ${track.activeCues.length}`);
            });
            
            track.addEventListener('load', () => {
                console.log(`load 事件触发 - 轨道 ${i}`);
            });
        }

        video.addEventListener('timeupdate', () => {
            if (dotNetObject) {
                dotNetObject.invokeMethodAsync('OnVideoTimeUpdate', video.currentTime);
            }
        });
    },
    
    updateDebugSubtitle: (text) => {
        let debugSubtitle = document.getElementById('debug-subtitle');
        if (!debugSubtitle) {
            debugSubtitle = document.createElement('div');
            debugSubtitle.id = 'debug-subtitle';
            debugSubtitle.style.cssText = `
                background-color: rgba(255, 165, 0, 0.8);
                color: #ffffff;
                padding: 8px 12px;
                border-radius: 4px;
                font-size: 14px;
                font-family: 'Microsoft YaHei', sans-serif;
                text-shadow: 1px 1px 2px rgba(0, 0, 0, 0.8);
                max-width: 800px;
                text-align: center;
            `;
            const container = document.getElementById('custom-subtitle-container');
            if (container) {
                container.appendChild(debugSubtitle);
            }
        }
        debugSubtitle.textContent = text;
    }
};

function updateSubtitleDisplay(video, trackIndex) {
    const track = video.textTracks[trackIndex];
    
    if (!track || !track.activeCues || track.activeCues.length === 0) {
        return;
    }
    
    const activeCue = track.activeCues[0];
    
    console.log(`更新字幕 ${trackIndex}:`, {
        trackLabel: track.label,
        trackLang: track.language,
        activeCues: track.activeCues.length,
        activeCue: activeCue,
        cueText: activeCue ? activeCue.text : null
    });
    
    let subtitleElement = document.getElementById(`subtitle-track-${trackIndex}`);
    if (!subtitleElement) {
        subtitleElement = document.createElement('div');
        subtitleElement.id = `subtitle-track-${trackIndex}`;
        subtitleElement.style.cssText = `
            background-color: rgba(0, 0, 0, 0.7);
            color: #ffffff;
            padding: 6px 12px;
            border-radius: 4px;
            font-size: 18px;
            font-family: 'Microsoft YaHei', sans-serif;
            text-shadow: 1px 1px 2px rgba(0, 0, 0, 0.8);
            text-align: center;
            line-height: 1.4;
        `;
        const container = document.getElementById('custom-subtitle-container');
        if (container) {
            container.appendChild(subtitleElement);
            console.log(`创建字幕元素 ${trackIndex}`);
        }
    }
    
    if (activeCue) {
        subtitleElement.textContent = activeCue.text;
        subtitleElement.style.display = 'block';
        console.log(`显示字幕 ${trackIndex}:`, activeCue.text);
    } else {
        subtitleElement.style.display = 'none';
    }
}

function updateAllSubtitles(video) {
    const textTracks = video.textTracks;
    for (let i = 0; i < textTracks.length; i++) {
        updateSubtitleDisplay(video, i);
    }
}
