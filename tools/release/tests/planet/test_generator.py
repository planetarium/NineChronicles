from datetime import datetime

import pytest

from toolbelt.tools.planet import generate_extra

timestamp = datetime.utcnow().strftime("%Y-%m-%d")


@pytest.mark.parametrize(
    "commit_map,reset_required,prev,except_result",
    [
        (
            # reset
            {
                "launcher": "86993a417d6e64ce902e916d680db14cc97653f1",
                "player": "884cc7966457d503ef6156f148cade634b72a1b",
            },
            True,
            {
                "timestamp": timestamp,
                "launcher": "2/86993a41dd6e64ce902e916d680db14cc97653f1",
                "player": "2/884cc7966d57d503ef6156f148cade634b72a1b",
            },
            {
                "timestamp": timestamp,
                "launcher": "1/86993a417d6e64ce902e916d680db14cc97653f1",
                "player": "1/884cc7966457d503ef6156f148cade634b72a1b",
            },
        ),
        (
            # not reset
            {
                "launcher": "asfaw3rgawr3abewaewfad",
                "player": "3rfa3r3rfra3vdfasdfadsf",
            },
            False,
            {
                "timestamp": timestamp,
                "launcher": "1/efeffasdfef02e916d680db14cc97653f1",
                "player": "1/adsfadf3ef6156f148cade634b72a1b",
            },
            {
                "timestamp": timestamp,
                "launcher": "2/asfaw3rgawr3abewaewfad",
                "player": "2/3rfa3r3rfra3vdfasdfadsf",
            },
        ),
        (
            # only one repo
            {
                "launcher": "asfaw3rgawr3abewaewfad",
                "player": "adsfadf3ef6156f148cade634b72a1b",
            },
            False,
            {
                "timestamp": timestamp,
                "launcher": "2/efeffasdfef02e916d680db14cc97653f1",
                "player": "1/adsfadf3ef6156f148cade634b72a1b",
            },
            {
                "timestamp": timestamp,
                "launcher": "3/asfaw3rgawr3abewaewfad",
                "player": "1/adsfadf3ef6156f148cade634b72a1b",
            },
        ),
        (
            # only one repo
            {
                "launcher": "efeffasdfef02e916d680db14cc97653f1",
                "player": "aefaef32gdfaesfasfsaf",
            },
            False,
            {
                "timestamp": timestamp,
                "launcher": "3/efeffasdfef02e916d680db14cc97653f1",
                "player": "5/adsfadf3ef6156f148cade634b72a1b",
            },
            {
                "timestamp": timestamp,
                "launcher": "3/efeffasdfef02e916d680db14cc97653f1",
                "player": "6/aefaef32gdfaesfasfsaf",
            },
        ),
        (
            # different shema
            {
                "launcher": "efeffasdfef02e916d680db14cc97653f1",
                "player": "aefaef32gdfaesfasfsaf",
            },
            False,
            {
                "timestamp": timestamp,
                "Windows": "1/efeffasdfef02e916d680db14cc97653f1",
            },
            {
                "timestamp": timestamp,
                "launcher": "1/efeffasdfef02e916d680db14cc97653f1",
                "player": "1/aefaef32gdfaesfasfsaf",
            },
        ),
        (
            # None prev
            {
                "launcher": "efeffasdfef02e916d680db14cc97653f1",
                "player": "aefaef32gdfaesfasfsaf",
            },
            True,
            None,
            {
                "timestamp": timestamp,
                "launcher": "1/efeffasdfef02e916d680db14cc97653f1",
                "player": "1/aefaef32gdfaesfasfsaf",
            },
        ),
    ],
)
def test_generate_extra(commit_map, reset_required, prev, except_result):
    r = generate_extra(
        commit_map,
        reset_required,
        prev,
    )

    assert r == except_result


@pytest.mark.parametrize(
    "commit_map,reset_required,prev,err",
    [
        (
            # prev required
            {
                "launcher": "efeffasdfef02e916d680db14cc97653f1",
                "player": "aefaef32gdfaesfasfsaf",
            },
            False,
            None,
            AssertionError,
        ),
    ],
)
def test_generate_extra_failure(commit_map, reset_required, prev, err):
    with pytest.raises(err):
        generate_extra(
            commit_map,
            reset_required,
            prev,
        )
