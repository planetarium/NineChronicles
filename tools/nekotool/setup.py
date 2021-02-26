from setuptools import setup

setup(
    name="nekotool",
    entry_points = {
        'console_scripts': ['nekotool=nekotool:cli'],
    }
)
