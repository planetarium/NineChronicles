def is_iterable(v) -> bool:
    try:
        iter(v)
        return True
    except TypeError:
        pass

    return False
