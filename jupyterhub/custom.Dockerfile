FROM jupyter/scipy-notebook

COPY ./karlkim-setup.py /tmp

ARG INDEX_URL
RUN export PIP_EXTRA_INDEX_URL=$INDEX_URL \
    && python -m pip install --upgrade pip \
    && python -m pip install cvxopt \
    && python -m pip install pyDOE \
    && python -m pip install OSQP \
    && unset PIP_EXTRA_INDEX_URL 
CMD sh